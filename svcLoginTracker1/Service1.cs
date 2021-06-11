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
using MongoDB.Driver;

namespace svcLoginTracker1
{
    public partial class Service1 : ServiceBase
    {
        //Service Timer Info
        private static System.Timers.Timer m_mainTimer;
        private static int interval = 15 * 1000; //How often to run in milliseconds (seconds * 1000)
        private static string dbconnection = "mongodb://192.168.50.125:27017";
        private static string dbname = "logins";
        private static string dbcollection= "events";
        private static string livepath = @"C:\temp\logins\";
        private static string archivepath = @"C:\temp\logins\archive\";
        private static string filename = @"loginsV3.log";

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
            var events = ParseEvents(livepath, archivepath, filename);

            WriteEvents(events);
        }

        private List<Login> ParseEvents(string LivePath, string ArchivePath, string Filename)
        {
            var logins = new List<Login>(); //Create a list of login events to return
            var livefile = LivePath + Filename;
            var tempfile = LivePath + "tempfile";
            var archivefile = ArchivePath + DateTime.Now.ToString() + @"\tempfile";

            try
            {
                if (File.Exists(LivePath + Filename))
                {
                    //Rename the log file so new events can be written to a fresh file
                    System.IO.File.Move(livefile, tempfile);

                    //Parse the new tempfile for events
                    var file = File.ReadAllLines(tempfile);

                    //Iterate through each event to create a login object for each one
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
                        logins.Add(login); //Add the login object to the list
                    }

                    //Move and rename the tempfile to the archive folder
                    System.IO.File.Copy(tempfile, archivefile);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return logins; //return the list of logins
        }

        private DateTime ConvertTimestamp(string filetimestamp)
        {

            //This function is here because I concatenated a string to represent the date
            //and time on the client end and it ended up being a pain to parse on this end...

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

        private void WriteEvents(List<Login> Logins)
        {
            
            foreach(var login in Logins)
            {
                try
                {
                    if(!CheckForDupe(login))
                        WriteEvent(login);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private void WriteEvent(Login Login)
        {
            try
            {
                var dbClient = new MongoClient(dbconnection);
                var database = dbClient.GetDatabase(dbname);
                var collection = database.GetCollection<Login>(dbcollection);

                collection.InsertOne(Login);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private bool CheckForDupe(Login Login)
        {
            try
            {
                var dbClient = new MongoClient(dbconnection);
                var database = dbClient.GetDatabase(dbname);
                var collection = database.GetCollection<Login>(dbcollection);

                var found = collection.AsQueryable()
                    .Where(l => l.Timestamp == Login.Timestamp && l.LoginType == Login.LoginType && l.Username == Login.Username && l.Machine == Login.Machine).FirstOrDefault();

                if (found != null)
                    return true;
                else
                    return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

    }
}
