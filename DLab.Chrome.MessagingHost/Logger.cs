using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;

namespace BondiGeek.Logging
{
    /// <summary>
    /// A Logging class implementing the Singleton pattern and an internal Queue to be flushed perdiodically
    /// </summary>
    public class LogWriter
    {
        private static LogWriter instance;
        private static Queue<Log> logQueue;
        private static string logDir = @"c:\\temp\\";
        private static string logFile = @"dlab-log.txt";
        private static int maxLogAge = int.Parse("5");
        private static int queueSize = int.Parse("1");
        private static DateTime LastFlushed = DateTime.Now;
        private string _logPath;

        /// <summary>
        /// Private constructor to prevent instance creation
        /// </summary>
        private LogWriter()
        {
            _logPath = logDir + DateTime.Now.ToString("yyyy-MM-dd") + "_" + logFile;
            TryDelete(_logPath);
        }

        private void TryDelete(string file)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
        /// <summary>
        /// An LogWriter instance that exposes a single instance
        /// </summary>
        public static LogWriter Instance
        {
            get
            {
                // If the instance is null then create one and init the Queue
                if (instance == null)
                {
                    instance = new LogWriter();
                    logQueue = new Queue<Log>();
                }
                return instance;
            }
        }

        /// <summary>
        /// The single instance method that writes to the log file
        /// </summary>
        /// <param name="message">The message to write to the log</param>
        public void WriteToLog(string message)
        {
            // Lock the queue while writing to prevent contention for the log file
            lock (logQueue)
            {
                // Create the entry and push to the Queue
                Log logEntry = new Log(message);
                logQueue.Enqueue(logEntry);

                // If we have reached the Queue Size then flush the Queue
                if (logQueue.Count >= queueSize || DoPeriodicFlush())
                {
                    FlushLog();
                }
            }
        }

        private bool DoPeriodicFlush()
        {
            TimeSpan logAge = DateTime.Now - LastFlushed;
            if (logAge.TotalSeconds >= maxLogAge)
            {
                LastFlushed = DateTime.Now;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Flushes the Queue to the physical log file
        /// </summary>
        private void FlushLog()
        {
            while (logQueue.Count > 0)
            {
                Log entry = logQueue.Dequeue();

                // This could be optimised to prevent opening and closing the file for each write
                using (FileStream fs = File.Open(_logPath, FileMode.Append, FileAccess.Write))
                {
                    using (StreamWriter log = new StreamWriter(fs))
                    {
                        log.WriteLine(string.Format("{0}\t{1}", entry.LogTime, entry.Message));
                    }
                }
            }
        }
    }

    /// <summary>
    /// A Log class to store the message and the Date and Time the log entry was created
    /// </summary>
    public class Log
    {
        public string Message { get; set; }
        public string LogTime { get; set; }
        public string LogDate { get; set; }

        public Log(string message)
        {
            Message = message;
            LogDate = DateTime.Now.ToString("yyyy-MM-dd");
            LogTime = DateTime.Now.ToString("hh:mm:ss.fff tt");
        }
    }
}