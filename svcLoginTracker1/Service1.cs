using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using svcLoginTracker1.models;

namespace svcLoginTracker1
{
    public partial class Service1 : ServiceBase
    {
        //Service Timer Info
        private static System.Timers.Timer m_mainTimer;
        private static int interval = 5 * 1000; //How often to run in milliseconds (seconds * 1000)

        public Service1()
        {
            InitializeComponent();
        }

        public void OnDebug()
        {
            //Manually kick off the service when debugging
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            //Create the Main timer
            m_mainTimer = new System.Timers.Timer
            {
                //Set the timer interval
                Interval = interval
            };

            //Dictate what to do when the event fires
            m_mainTimer.Elapsed += m_mainTimer_Elapsed;

            //Something to do with something, I forgot since it's been a while
            m_mainTimer.AutoReset = true;

#if DEBUG
#else
            m_mainTimer.Start(); //Start timer only in Release
#endif
            //Run 1st Tick Manually
            Console.Beep();
            Routine();
        }

        protected override void OnStop()
        {
        }

        void m_mainTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Routine();
        }

        private void Routine()
        {
            ParseEvents();
        }

        private void ParseEvents()
        {
            var logins = new List<Login>();
            var path = @"C:\temp\logins\loginsV3.log";
            var tempPath = @"C:\temp\logins\tempfile";

            try
            {
                if (File.Exists(path))
                {
                    //Rename the log file so new events can be written
                    System.IO.File.Move(path, tempPath);

                    //Parse the new tempfile
                    var file = File.ReadAllLines(tempPath);

                    //Iterate through each event
                    foreach (var line in file)
                    {
                        var fields = line.Split(' ');

                        var guid = Guid.NewGuid();
                        var logintype = fields[0];
                        var username = fields[1];
                        var machine = fields[2];
                        var timestamp = ConvertTimestamp(fields[3]);
                        var domaincontroller = fields[4].Replace(@"\\", string.Empty);
                        var gateway = fields[5];

                        var login = new Login
                        {
                            Id = guid,
                            LoginType = logintype,
                            Username = username,
                            Machine = machine,
                            Timestamp = timestamp,
                            DomainController = domaincontroller,
                            Gateway = gateway
                        };
                        logins.Add(login);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private DateTime ConvertTimestamp(string filetimestamp)
        {
            var dt = filetimestamp.Replace("::", ",").Split(',');
            var date = dt[0].Split('-');
            var time = dt[1].Split(':');

            var year = Convert.ToInt32(date[0]);
            var month = Convert.ToInt32(date[1]);
            var day = Convert.ToInt32(date[2]);

            var hour = Convert.ToInt32(time[0]);
            var minute = Convert.ToInt32(time[1]);
            var second = (int)Convert.ToDouble(time[2]);

            var timestamp = new DateTime(year, month, day, hour, minute, second);

            return timestamp;
        }
    }
}
