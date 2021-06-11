using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace svcLoginTracker1.models
{
    public class Login
    {
        public Guid Id { get; set; }
        public string LoginType { get; set; }
        public string Username { get; set; }
        public string Machine { get; set; }
        public DateTime Timestamp { get; set; }
        public string DomainController { get; set; }
        public string Gateway { get; set; }
    }
}
