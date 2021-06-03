using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Analyser
{
    // This class will provide a stream to read logs from
    public class LogStream
    {
        private List<string> columnNames;
        public Queue<Log> QueuedLogs;
        private string _primaryKeyColumn;
        private double _primaryKeyScale;

        private Log _previousLog;
        StreamReader _file;

        public LogStream(string filePath, string primaryKeyColumn, double primaryKeyScale)
        {
            QueuedLogs = new Queue<Log>();
            _file = new StreamReader(filePath);
            _primaryKeyColumn = primaryKeyColumn;
            _primaryKeyScale = primaryKeyScale;

            columnNames = _file.ReadLine().Split(';').ToList();
        }

        public Log GetNextLog()
        {
            var log = new Log();
            var line = _file.ReadLine();
            if (line == null)
            {
                return null;
            }

            var values = line.Split(';').ToList();
            for (int i = 0; i < columnNames.Count; i++)
            {
                string value = values[i];
                if (columnNames[i] == _primaryKeyColumn)
                {
                    value = (double.Parse(value) / _primaryKeyScale).ToString();
                }

                log.Add(columnNames[i], value);

            }


            log.ChangedColumns = _previousLog == null ? new List<string>() : GetChangedColumns(log).ToList();
            _previousLog = log;

            return log;
        }

        /// <summary>
        /// gets the last log where the signal is changed
        /// </summary>
        /// <param name="signalName"></param>
        /// <returns></returns>
        public IEnumerable<Log> GetChangedLogs(string signalName)
        {
            return QueuedLogs
                .Where(x => x.ChangedColumns.Any(x=> x.Equals(signalName, StringComparison.OrdinalIgnoreCase)));
        }

        // get columns that have changed 
        private IEnumerable<string> GetChangedColumns(Log currentLog)
        {
            foreach (var keyValue in currentLog)
            {
                // if signal has a logged value, it means it has changed. 
                if (!keyValue.Key.Equals(_primaryKeyColumn, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(keyValue.Value)) //timestamp is ignored because it changes always
                {
                    yield return keyValue.Key;
                }
            }
        }

        //public void Enqueue(Log log)
        //{
        //    Logs.Enqueue(log);
        //}

        //public void Dequeue(Log log)
        //{
        //    Logs.Dequeue();
        //}
    }
}