using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.TaskScheduler;
using System.IO;

namespace PassWatch
{
    class Program
    {
        // Sets whether additional debug information will be added to the log file.
        static bool debug = true;

        // Sets the log file path.
        static string logPath = @"C:\Users\Public\Documents\Logs\PassWatch_Log.txt";

        static void Main(string[] args)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            // *** Install *** //
            if (args.Contains("-install"))
            {
                try
                {
                    using (TaskService ts = new TaskService())
                    {
                        TaskDefinition td = ts.NewTask();
                        td.RegistrationInfo.Description = "Keeps Credential Manager free of unwanted credentials.";
                        td.Triggers.Add(new IdleTrigger() { ExecutionTimeLimit = TimeSpan.FromSeconds(5) });
                        td.Actions.Add(new ExecAction(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\PassWatch\PassWatch.exe"));

                        ts.RootFolder.RegisterTaskDefinition("PassWatch", td);
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            // *** Uninstall *** //
            else if (args.Contains("-uninstall"))
            {
                try
                {
                    using (TaskService ts = new TaskService())
                    { 
                        ts.RootFolder.DeleteTask("PassWatch");
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else
            {
                // *** Main Program *** //
            }
        }
    }
}
