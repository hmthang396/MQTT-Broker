using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Broker
{
    internal class Configuration
    {
        public Configuration() { }
        public string Username { get; set; }
        public string Password { get; set; }
        public string MongoDBUrl { get; set; }
        public string Databases { get; set; }
        public string Collection { get; set; }
        public int Port { get; set; }

    }
}
