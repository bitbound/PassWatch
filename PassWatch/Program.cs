using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.TaskScheduler;
using System.IO;
using PassWatch.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PassWatch
{
    class Program
    {
        static Config config { get; set; }
        static System.Reflection.Assembly assem = System.Reflection.Assembly.GetExecutingAssembly();
        static void Main(string[] args)
        {

// *** Initialization *** //

            // Global error handler.
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Initialize configuration.
            if (File.Exists(@".\PassWatch_Config.json"))
            {
                try
                {
                    config = JsonHelper.Decode<Config>(File.ReadAllText(@".\PassWatch_Config.json"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error parsing config file.  Check log for details.");
                    writeToLog("Error parsing config file.  " + ex.ToString());
                    // Use default configuration if parsing fails.
                    config = new Config();
                }
            }
            // Use default configuration if file doesn't exist.
            else
            {
                config = new Config();
            }

            // Convert string[] to List<> for extension methods.
            var argList = args.ToList();

            // Ensure log path exists.
            Directory.CreateDirectory(Path.GetDirectoryName(config.LogPath));

// *** Update *** //
            if (argList.Exists(str => str.ToLower() == "-update"))
            {
                var updateLocation = argList[argList.FindIndex(str => str.ToLower() == "-update") + 1];
                updatePassWatch(updateLocation);
                return;
            }

// *** Install *** //
            else if (argList.Exists(str => str.ToLower() == "-install"))
            {
                if (argList.Exists(str => str.ToLower() == "-force"))
                {
                    installPassWatch(true);
                }
                else
                {
                    installPassWatch(false);
                }
            }

// *** Reinstall *** //
            else if (argList.Exists(str => str.ToLower() == "-reinstall"))
            {
                installPassWatch(true);
            }

// *** Uninstall *** //
            else if (argList.Exists(str => str.ToLower() == "-uninstall"))
            {
                uninstallPassWatch();
            }
            else
            {
                // Invalid switch.  Show help.
                if (argList.Count > 0)
                {
                    var streamConsoleHelp = assem.GetManifestResourceStream("PassWatch.Assets.ConsoleHelp.txt");
                    Console.Write(new StreamReader(streamConsoleHelp).ReadToEnd());
                    streamConsoleHelp.Close();
                    return;
                }
                // Check for updates if auto update is configured.
                if (config.AutoUpdateServiceURI != null && config.AutoUpdateFileURI != null)
                {
                    // Close if update available and started.
                    var result = checkForUpdates();
                    result.Wait();
                    if (result.Result)
                    {
                        return;
                    }
                }

// *** Main Program *** //

                // Retrieve credential list.
                var listCreds = getCredList();
                var listCredsToRemove = new List<Credential>();
                // If credential types specified in config, find matching ones.
                if (config.RemoveCredentialTypes?.Count > 0)
                {
                    var listRemove = listCreds.FindAll(cred => config.RemoveCredentialTypes.Contains(cred.Type));
                    listCredsToRemove.AddRange(listRemove);
                }
                // If target keywords specified in config, find matching ones.
                if (config.RemoveCredentialTargets?.Count > 0)
                {
                    var listRemove = listCreds.FindAll(cred => config.RemoveCredentialTargets.Contains(cred.Target));
                    listCredsToRemove.AddRange(listRemove);
                }
                // Remove each credential found.
                foreach (var cred in listCredsToRemove)
                {
                    Console.WriteLine("Removing credential " + cred.Target);
                    writeToLog("Removing credential " + cred.Target);
                    var psi = new ProcessStartInfo("cmdkey", "/delete:" + cred.Target);
                    psi.CreateNoWindow = false;
                    psi.WindowStyle = ProcessWindowStyle.Hidden;
                    Process.Start(psi);
                }
            }
        }

        // Global error handler.
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("Unhandled Error: " + (e.ExceptionObject as Exception).ToString());
            writeToLog((e.ExceptionObject as Exception).ToString());
        }

        // Parse results of "cmdkey /list" into a List<Credential>.
        private static List<Credential> getCredList()
        {
            var psi = new ProcessStartInfo("cmdkey", "/list");
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            var proc = Process.Start(psi);
            proc.WaitForExit();
            var strCreds = proc.StandardOutput.ReadToEnd();
            var listStrCreds = strCreds.Split((Environment.NewLine).ToCharArray()).ToList();
            listStrCreds.RemoveAll(str => str.Trim().Length == 0);
            listStrCreds.RemoveAll(str => str.StartsWith("Currently"));
            listStrCreds.RemoveAll(str => str.StartsWith("Local"));
            Credential newCred = null;
            var listCreds = new List<Credential>();
            for (var i = 0; i < listStrCreds.Count; i++)
            {
                listStrCreds[i] = listStrCreds[i].Trim();
                if (listStrCreds[i].StartsWith("Target:"))
                {
                    if (newCred != null)
                    {
                        listCreds.Add(newCred);
                        newCred = new Credential();
                    }
                    newCred = new Credential();
                    listCreds.Add(newCred);
                    newCred.Target = listStrCreds[i].Split(":".ToCharArray(), 2)[1].Trim();
                }
                else if (listStrCreds[i].StartsWith("Type:"))
                {
                    newCred.Type = listStrCreds[i].Split(":".ToCharArray(), 2)[1].Trim();
                }
                else if (listStrCreds[i].StartsWith("User:"))
                {
                    newCred.User = listStrCreds[i].Split(":".ToCharArray(), 2)[1].Trim();
                }
            }
            return listCreds;
        }
        
        private static void installPassWatch(bool force)
        {
            // Return if already installed and force install isn't set.
            if (!force && File.Exists(config.InstallFolder + "PassWatch.exe") && Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false).GetValue("PassWatch Install") != null)
            {
                return;
            }
            try
            {
                // Remove existing directory and files, if present.
                if (Directory.Exists(config.InstallFolder))
                {
                    Directory.Delete(config.InstallFolder, true);
                }
                Directory.CreateDirectory(config.InstallFolder);
                // Copy files to install folder.
                File.Copy(assem.Location, config.InstallFolder + @"\PassWatch.exe");
                if (File.Exists(@".\PassWatch_Config.json"))
                {
                    File.Copy(@".\PassWatch_Config.json", config.InstallFolder + "PassWatch_Config.json");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error copying files.  Check log for details.");
                writeToLog("Error copying files.  " + ex.ToString());
                return;
            }
            try
            {
                // Create and register task.
                using (TaskService ts = new TaskService())
                {
                    TaskDefinition td = ts.NewTask();
                    td.RegistrationInfo.Description = "Keeps Credential Manager free of unwanted credentials.";
                    td.Triggers.Add(new IdleTrigger() { ExecutionTimeLimit = TimeSpan.FromSeconds(5) });
                    td.Triggers.Add(new LogonTrigger() { ExecutionTimeLimit = TimeSpan.FromSeconds(5), Repetition = new RepetitionPattern(TimeSpan.FromHours(1), TimeSpan.Zero) });
                    td.Actions.Add(new ExecAction(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\PassWatch\PassWatch.exe"));
                    ts.RootFolder.RegisterTaskDefinition("PassWatch", td);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error registering scheduled task.  Check log for details.");
                writeToLog("Error registering scheduled task.  " + ex.ToString());
                return;
            }
            try
            {
                // Add registry key that will install for each user.
                var runKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                runKey.SetValue("PassWatch Install", config.InstallFolder + "PassWatch.exe -install");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing registry key.  Check log for details.");
                writeToLog("Error writing registry key.  " + ex.ToString());
                return;
            }
            Console.WriteLine("Install successful.");
            writeToLog("Install successful.");
        }
        private static void uninstallPassWatch()
        {
            // Remove task.
            try
            {
                using (TaskService ts = new TaskService())
                {
                    ts.RootFolder.DeleteTask("PassWatch");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error removing task.  Check log for details.");
                writeToLog("Error removing task.  " + ex.ToString());
            }
            // Remove directory and files.
            try
            {
                if (Directory.Exists(config.InstallFolder))
                {
                    Directory.Delete(config.InstallFolder, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting files.  Check log for details.");
                writeToLog("Error deleting files.  " + ex.ToString());
                return;
            }
            // Remove registry key.
            try
            {
                var runKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                runKey.DeleteValue("PassWatch Install", false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error removing registry key.  Check log for details.");
                writeToLog("Error removing registry key.  " + ex.ToString());
                return;
            }
            Console.WriteLine("Uninstall successful.");
            writeToLog("Uninstall successful.");
        }
        private static void writeToLog (string message)
        {
            File.AppendAllText(config.LogPath, DateTime.Now.ToString() + ": " + message + Environment.NewLine + Environment.NewLine);
        }
        
        private static async System.Threading.Tasks.Task<bool> checkForUpdates()
        {
            System.Net.WebClient webClient = new System.Net.WebClient();
            System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
            System.Net.Http.HttpResponseMessage result = null;
            var strTempPath = Path.GetTempPath() + "PassWatch.exe";
            string strServerVersion = null;

            // Get server/remote version.
            if (config.AutoUpdateServiceURI.StartsWith("http"))
            {
                result = await httpClient.GetAsync(config.AutoUpdateServiceURI);
                strServerVersion = await result.Content.ReadAsStringAsync();
            }
            else if (File.Exists(config.AutoUpdateFileURI))
            {
                strServerVersion = FileVersionInfo.GetVersionInfo(config.AutoUpdateFileURI).FileVersion;
            }

            var serverVersion = Version.Parse(strServerVersion);

            var thisVersion = assem.GetName().Version;

            // Check if update is available.
            if (serverVersion > thisVersion)
            {
                // Remove temp file if exists.
                if (File.Exists(strTempPath))
                {
                    File.Delete(strTempPath);
                }
                // Get updated version.
                try
                {
                    // Download if URI is HTTP.
                    if (config.AutoUpdateFileURI.StartsWith("http"))
                    {
                        await webClient.DownloadFileTaskAsync(new Uri(config.AutoUpdateFileURI), strTempPath);
                    }
                    // Copy if URI is UNC path.
                    else if (File.Exists(config.AutoUpdateFileURI))
                    {
                        File.Copy(config.AutoUpdateFileURI, strTempPath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error updating PassWatch.  See log for details.");
                    writeToLog("Error updating PassWatch.  " + ex.ToString());
                }
                // Start update process from temp file.
                Process.Start(strTempPath, "-update \"" + assem.Location + "\"");
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void updatePassWatch(string originalLocation)
        {
            // Check if old version file exists and it's the same file as this one.
            if (File.Exists(originalLocation) && Path.GetFileName(originalLocation) == Path.GetFileName(assem.Location))
            {
                var startTime = DateTime.Now;
                var success = false;
                // Try to replace old file for 30 seconds.  This gives time for the old file's process to close.
                while (DateTime.Now - startTime < TimeSpan.FromSeconds(30) && !success)
                {
                    try
                    {
                        File.Copy(assem.Location, originalLocation, true);
                        success = true;
                    }
                    catch
                    {
                        success = false;
                    }
                    System.Threading.Thread.Sleep(500);
                }
                // Report success or failure.
                if (success == false)
                {
                    Console.WriteLine("Update failed.");
                    writeToLog("Update failed.");
                }
                else
                {
                    Console.WriteLine("Update successful.");
                    writeToLog("Update successful.");
                    Process.Start(originalLocation);
                }
                return;
            }
        }
    }
}
