using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Threading;
using CoreSystem.Util;
using System.IO;
using System.Xml.Linq;

namespace LogRotator
{
    /// <summary>
    /// Rotator service to compressing and deleting files based on match patterns
    /// </summary>
    public class Rotator
    {
        #region XML tags

        private const string XML_LOG_ROTATOR = "logRotator";

        private const string XML_POOL_INTERVAL = "poolInterval";

        #endregion

        private static readonly ILog Logger = LogManager.GetLogger(typeof(Rotator));

        private bool continuePooling;

        private int poolInterval;

        private Pattern[] patterns;

        private Thread workerThread;

        /// <summary>
        /// Initializes Rotator service with pool interval and match patterns
        /// </summary>
        /// <param name="poolInterval">Interval in milliseconds between each lookup operation for files to compress or delete</param>
        /// <param name="patterns">Patterns containing information for files to search for</param>
        public Rotator(int poolInterval, Pattern[] patterns)
        {
            if (poolInterval < 1)
                throw new ArgumentException(string.Format("PoolInterval is invalid: '{0}'", poolInterval));

            if (patterns == null || patterns.Length == 0)
                throw new ArgumentException("Atleast one match patterns should be provided");

            this.poolInterval = poolInterval;
            this.patterns = patterns;
        }

        public void Start()
        {
            if (this.workerThread != null)
                throw new InvalidOperationException("LogRotator service is already running");

            this.continuePooling = true;
            this.workerThread = new Thread(new ThreadStart(this.Do));
            this.workerThread.Start();
            Logger.InfoFormat("LogRotator service started");
        }

        public void Stop()
        {
            if (this.workerThread != null)
            {
                this.continuePooling = false;
                this.workerThread.Interrupt();
                this.workerThread.Join();
                this.workerThread = null;

                Logger.InfoFormat("LogRotator service is stopped");
            }
        }

        private void Do()
        {
            while (this.continuePooling)
                try
                {
                    foreach (var pattern in this.patterns)
                    {
                        try
                        {
                            pattern.Do();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                        }
                    }

                    Thread.Sleep(this.poolInterval);
                }
                catch (ThreadInterruptedException)
                { }
        }

        public static Rotator Parse(string path)
        {
            Guard.CheckNullOrWhiteSpace(path, "Path cannot be empty");

            if (!File.Exists(path))
                throw new ArgumentException("LogRotator configuration file doesn't exists: " + path);

            XElement logRotator;
            try { logRotator = XElement.Parse(File.ReadAllText(path)); }
            catch (Exception ex) { throw new InvalidOperationException("LogRotator configuration file is not valid XML", ex); }

            if (logRotator.Name != XML_LOG_ROTATOR)
                throw new InvalidOperationException(string.Format("Invalid LogRotator configuration file, root element must be 'logRotator', where as found '{0}'", logRotator.Name));

            var poolInterval = (int)logRotator.GetMandatoryAttribute(XML_POOL_INTERVAL);
            var patterns = logRotator.Elements(Pattern.XML_PATTERN).Select(p => Pattern.Parse(p)).ToArray();

            return new Rotator(poolInterval, patterns);
        }
    }
}
