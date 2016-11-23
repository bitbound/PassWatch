# PassWatch
PassWatch is a console application that keeps the Windows Credential Manager free of unwanted credentials.  It features auto-updating capabilities, Task Scheduler integration, and an optional, deployable config file.  It can be run as a stand-alone EXE or installed as a scheduled task.

### Installation
Use the -install switch from an elevated command prompt to install PassWatch.  The file is copied to "%programdata%\PassWatch".  A task is scheduled at the root level that will run PassWatch on login, on idle, and every hour.  A registry value is set in HKLM\Software\Microsoft\Windows\CurrentVersion\Run that makes sure PassWatch is installed for each user at login.

### Other Switches
  * -force = When combined with -install, this will force an overwrite of the files.  Same as -reinstall.
  * -reinstall = Same as above.
  * -uninstall = Removes the folder and files in %programdata%\PassWatch, removes the scheduled task, and deletes the registry key.
  * -update = Used by the auto-update feature.  The next argument must be the path the old PassWatch file.

### Configuration
Upon install, PassWatch will look for a file in the current directory named "PassWatch_Config.json" to use for its configuration.  The file must contain JSON data representing the class Models\Config.cs.  If successful, PassWatch will be installed with the configuration data.  If parsing fails, it will fall back to the default configuration.

### Auto Updating
To enable auto-updating, specify the URIs in the config.  The URIs can either be http/https or a filesystem path (UNC is supported).  If using http/s, the ServiceURI must respond to a GET request with a version number that can be parsed by System.Version.Parse(), and the FileURI must be the actual EXE file to be downloaded.

### Filtering
By default, PassWatch removes any credential of type "Generic" or "Generic Certificate."  You can change the Types and/or specify a Targets keyword.  If a Targets keyword is specified, wildcards will be used automatically, so you can use partial strings.

### Logging
Unless otehrwise set, the log file will be in C:\Users\Public\Documents\Logs\PassWatch_Log.txt.  Check here if you encounter any issues.

### Third Party Libraries
TaskScheduler: http://taskscheduler.codeplex.com<br/>
Fody/Costura: https://github.com/Fody/Costura
