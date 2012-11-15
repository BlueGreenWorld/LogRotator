using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using log4net;
using System.IO;
using System.Reflection;

namespace LogRotator.WinService
{
    public partial class LogRotator : ServiceBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LogRotator));

        private static readonly string AssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private Rotator rotator;

        public LogRotator()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                this.rotator = Rotator.Parse(Path.Combine(AssemblyDir, Constants.ConfigFile));
                this.rotator.Start();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to start LogRotator service", ex);
            }
        }

        protected override void OnStop()
        {
            if (this.rotator != null)
                try
                {
                    this.rotator.Stop();
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to stop LogRotator service", ex);
                }
        }
    }
}
