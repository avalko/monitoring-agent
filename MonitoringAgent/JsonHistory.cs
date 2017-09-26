using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace MonitoringAgent
{
    public struct HistoryItem
    {
        public static HistoryItem Null { get; } = new HistoryItem() { Time = DateTime.MinValue };

        public int TimeStamp => (int)Time.Subtract(JsonHistory.DateStartEpoch).TotalSeconds;
        public DateTime Time { get; set; }
        public string Json { get; set; }

        public static bool operator ==(HistoryItem a, HistoryItem b)
        {
            return a.Time == b.Time;
        }

        public static bool operator !=(HistoryItem a, HistoryItem b)
        {
            return a.Time != b.Time;
        }

        public override int GetHashCode()
        {
            return Time.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is HistoryItem)
                return Time.Equals(((HistoryItem)obj).Time);

            return false;
        }
    }

    public class JsonHistory : IDisposable
    {
        public const string HISTORY_PATH = "history.dat";
        private const int HEADER_LENGTH = 32;

        private LinkedList<HistoryItem> _history;
        public static readonly DateTime DateStartEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        private DateTime _lastHistorySave = DateTime.MinValue;
        private int _inMemory = 0;
        
        private byte[] _currentHeader = new byte[HEADER_LENGTH];

        public JsonHistory()
        {
            _Init();
        }

        public void Insert(string json)
        {
            _history.AddFirst(new HistoryItem() { Time = DateTime.UtcNow, Json = json });
            ++_inMemory;
        }

        public IEnumerable<HistoryItem> Take(int last)
        {
            return _history.Take(last);
        }

        public void Flush()
        {
            try
            {
                var items = _history.Take(_inMemory);
                _inMemory = 0;
                int itemsCount = items.Count();
                if (itemsCount > 0)
                {
                    try
                    {
                        using (var stream = File.Open(HISTORY_PATH, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                        {
                            int countOfHistoryItems = 0;

                            if (stream.Length >= 4)
                            {
                                // We read the first 4 bytes (int32) from the file.
                                stream.Read(_currentHeader, 0, HEADER_LENGTH);
                                // This will be the count of entries in the file.
                                countOfHistoryItems = BitConverter.ToInt32(_currentHeader, 0);
                            }

                            // Reset stream position.
                            stream.Position = 0;
                            using (BinaryWriter bw = new BinaryWriter(stream))
                            {
                                // Updating count of entries.
                                bw.Write(countOfHistoryItems + itemsCount);

                                // Goto end of file.
                                bw.BaseStream.Position = bw.BaseStream.Length;

                                // Adding all new entries.
                                foreach (var item in items)
                                {
                                    bw.Write(item.TimeStamp);
                                    bw.Write(item.Json);
                                }

                                // Update file.
                                bw.Flush();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Warning($"Can't read history: {e}");
                    }
                }
            }
            catch { }
        }

        public void ClearHistoryIfOverflow()
        {
            while (_history.Count > Agent.Settings.SaveHistorySeconds)
            {
                _history.RemoveLast();
            }
        }

        public void AutoSave()
        {
            if ((DateTime.Now - _lastHistorySave) > TimeSpan.FromSeconds(Agent.Settings.AutoSaveHistorySeconds))
            {
                Flush();
                _lastHistorySave = DateTime.Now;
            }
        }

        public void Dispose()
        {
            Flush();
        }

        private void _Init()
        {
            _history = new LinkedList<HistoryItem>();
            if (File.Exists(HISTORY_PATH))
            {
                try
                {
                    // IMPORTANT!
                    // I don't use the BinaryReader specifically. Kick me if this code is less productive.
                    using (var stream = File.OpenRead(HISTORY_PATH))
                    {
                        // We read the first 4 bytes (int32) from the file.
                        stream.Read(_currentHeader, 0, HEADER_LENGTH);
                        // This will be the count of entries in the file.
                        int countOfHistoryItems = BitConverter.ToInt32(_currentHeader, 0);

                        byte[] dataBuffer = new byte[4096];
                        byte[] numberBuffer = new byte[8];
                        int currentTimestamp, currentDataLength;

                        // Then we count how many records we need to skip.
                        int diff = countOfHistoryItems - Agent.Settings.AutoSaveHistorySeconds;

                        // Each entry is:
                        // The first 4 bytes (int32) is a timestamp.
                        // The second 4 bytes (int32) is the length of json.
                        // Next <json> arbitrary length (which is stored in the second 4 bytes...)

                        // Performance! :D
                        while (diff-- > 0)
                        {
                            // Skip length of timestamp (int32 = 4 bytes).
                            stream.Position += 4;
                            // Reading length of json.
                            stream.Read(numberBuffer, 0, 4);
                            currentDataLength = BitConverter.ToInt32(numberBuffer, 0);
                            // Skip length of json.
                            stream.Position += currentDataLength;
                        }

                        while (stream.Position < stream.Length)
                        {
                            stream.Read(numberBuffer, 0, 8);
                            // Reading timestamp.
                            currentTimestamp = BitConverter.ToInt32(numberBuffer, 0);
                            // Reading length of json.
                            currentDataLength = BitConverter.ToInt32(numberBuffer, 4);

                            if (dataBuffer.Length < currentDataLength)
                                // Increasing the size of the buffer if necessary.
                                Array.Resize(ref dataBuffer, currentDataLength);

                            // Reading JSON.
                            stream.Read(dataBuffer, 0, currentDataLength);
                            string json = Encoding.UTF8.GetString(dataBuffer, 0, currentDataLength);

                            // Adding a new history entry.
                            _history.AddFirst(new HistoryItem() { Json = json, Time = DateStartEpoch.AddSeconds(currentTimestamp) });
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Warning($"Can't read history: {e}");
                }
            }
        }
    }
}
