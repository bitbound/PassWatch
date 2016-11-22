using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace PassWatch.Models
{
    public class Credential
    {
        public string Target { get; set; }
        public string Type { get; set; }
        public string User { get; set; }
    }
}
