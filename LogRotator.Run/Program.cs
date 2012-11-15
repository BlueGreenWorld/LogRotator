using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogRotator.Run
{
    class Program
    {
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            var rotator = Rotator.Parse(Constants.ConfigFile);
            rotator.Start();
            Console.WriteLine("Press Return to stop service");
            Console.ReadLine();
            rotator.Stop();
        }
    }
}
