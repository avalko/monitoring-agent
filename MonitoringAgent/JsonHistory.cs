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

        private List<HistoryItem> _history;
        public static readonly DateTime DateStartEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        private DateTime _lastHistorySave = DateTime.MinValue;
        private int _flushedToHistory = 0;

        public JsonHistory()
        {
            _Init();
        }

        public void Insert(string json)
        {
            _history.Insert(0, new HistoryItem() { Time = DateTime.UtcNow, Json = json });
        }

        public IEnumerable<HistoryItem> Take(int last)
        {
            return _history.Take(last);
        }

        public void ClearHistoryIfOverflow()
        {
            if (_history.Count > Agent.Settings.SaveHistorySeconds)
            {
                _history.RemoveRange(0, _history.Count - Agent.Settings.SaveHistorySeconds);
            }
        }

        public void Flush()
        {
            try
            {
                var items = _history.Skip(_flushedToHistory).Select(x => $"{x.TimeStamp};{x.Json}");
                if (items.Count() > 0)
                {
                    _flushedToHistory += items.Count();
                    File.AppendAllText(HISTORY_PATH, items.JoinString("\n"));
                }
            }
            catch { }
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
            if (File.Exists(HISTORY_PATH))
            {
                try
                {
                    var lines = File.ReadAllLines(HISTORY_PATH);
                    Log.Info($"History loaded ({TimeSpan.FromSeconds(lines.Length).ToString("c", CultureInfo.InvariantCulture)}).");
                    _history = lines.Select(line =>
                    {
                        var arr = line.Split(';');
                        if (arr.Length < 2)
                            return HistoryItem.Null;
                        return new HistoryItem()
                        {
                            Time = DateStartEpoch.AddSeconds(int.Parse(arr[0])),
                            Json = arr[1]
                        };
                    }).Where(x => !x.Equals(HistoryItem.Null)).ToList();
                    _flushedToHistory = _history.Count;
                }
                catch (Exception e)
                {
                    _history = new List<HistoryItem>();
                    Log.Warning($"Can't read history: {e}");
                }
            }
            else
            {
                _history = new List<HistoryItem>();
            }

            ClearHistoryIfOverflow();
        }
    }
}
