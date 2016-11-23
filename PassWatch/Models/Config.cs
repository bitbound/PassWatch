using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PassWatch.Models
{
    public class Config
    {
        public string InstallFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\PassWatch\";

        // Sets the log file path.
        public string LogPath { get; set; } = Path.GetPathRoot(Environment.SystemDirectory) + @"Users\Public\Documents\Logs\PassWatch_Log.txt";

        // URI that will respond to an HTTP GET request with a version number that can be parsed by System.Version.Parse().
        public string AutoUpdateServiceURI { get; set; }

        // URI to the remote EXE to download if new version is available.
        public string AutoUpdateFileURI { get; set; }

        // The credential types to remove.
        public List<string> RemoveCredentialTypes { get; set; } = new List<string>() { "Generic", "Generic Certificate" };

        // The target keywords to remove.  Wildcards are used on both sides of the string.
        public List<string> RemoveCredentialTargets { get; set; }
    }
}
