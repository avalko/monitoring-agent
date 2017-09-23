using System;
using System.Collections.Generic;
using System.Text;

namespace MonitoringAgent
{
    class ScanfPatternCountException  : Exception
    {
        public ScanfPatternCountException(int patternCount, int argumentsCount)
            : base($"The pattern contains fewer ({patternCount}) arguments than expected ({argumentsCount}).\n" +
                   "It is impossible to determine their type!")
        {

        }
    }
}
