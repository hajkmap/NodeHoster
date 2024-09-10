using Microsoft.Win32;
using Serilog;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace NodeHoster.Utils
{
    public class NodeInstallationUtility
    {

        private static string GetProgramEntryPoint()
        {
            string entryPath = Assembly.GetEntryAssembly().Location;
            return Path.GetDirectoryName(entryPath);
        }



        private static string GetNodeVersion()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("node", "-v");
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;

            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            string nodeVersion = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode == 0)
            {
                // Successfully executed node -v command
                return nodeVersion;
            }
            else
            {
                // Failed to execute node -v command
                return null;
            }
        }

        private bool SufficientNodeVersionIsInstalled()
        {

            string nodeVersion = GetNodeVersion();

            if (string.IsNullOrEmpty(nodeVersion))
            {
                Log.Error("[NodeInstallationUtility] Could not extract the Node version installed on the machine.");
                return false;
            }

            try
            {
                float cleanedNodeVersion = float.Parse(nodeVersion.Substring(1, 4), CultureInfo.InvariantCulture);
                return cleanedNodeVersion >= float.Parse((ConfigurationUtility.GetSectionItem("NodeHost:MinimumVersion") ?? "16"), CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                Log.Error("[NodeInstallationUtility] Could not extract the Node version installed on the machine.");
                return false;
            }

        }

        private static string GetNodeFolderName()
        {
            return ConfigurationUtility.GetSectionItem("NodeHost:FolderName") ?? "node_server";
        }

        private bool NodeModulesExists()
        {
            string entryPoint = GetProgramEntryPoint();
            string folderName = GetNodeFolderName();
            string nodeModulesPath = Path.Combine(entryPoint, folderName, "node_modules");
            return Directory.Exists(nodeModulesPath);

        }

        private static async Task InstallNodeModules()
        {
            string folderName = GetNodeFolderName();
            string nodeServerPath = Path.Combine(GetProgramEntryPoint(), folderName);
            ProcessStartInfo startInfo = new ProcessStartInfo("npm", "ci");
            startInfo.WorkingDirectory = @nodeServerPath;
            startInfo.UseShellExecute = true;

            Process process = new Process();
            process.StartInfo = startInfo;

            try
            {
                Log.Information("[NodeInstallationUtility] Trying to install Node modules...");

                process.Start();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    string exitCode = process.ExitCode.ToString();
                    Log.Error("[NodeInstallationUtility] Node modules could not be installed... (Exit code: {@exitCode})", exitCode);
                    throw new Exception("[NodeInstallationUtility] Node modules could not be installed...");
                }
                else
                {
                    Log.Information("[NodeInstallationUtility] Node modules has been installed successfully!");
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                string errorMessage = ex.Message;
                Log.Error("[NodeInstallationUtility] Error when installing Node modules. {@errorMessage}", errorMessage);
                Environment.Exit(-1);
            }



        }

        private static void SetupNodeProcessAndListeners()
        {
            Log.Information("[NodeInstallationUtility] Starting Node Server!");
            string folderName = GetNodeFolderName();
            string entryPoint = ConfigurationUtility.GetSectionItem("NodeHost:EntryPoint") ?? "index.js";
            string serverPath = @Path.Combine(GetProgramEntryPoint(), folderName, entryPoint);
            string serverDir = @Path.Combine(GetProgramEntryPoint(), folderName);
            ProcessStartInfo startInfo = new ProcessStartInfo("node", serverPath);
            startInfo.WorkingDirectory = serverDir;
            startInfo.UseShellExecute = false;

            Process process = new Process();
            process.StartInfo = startInfo;
            try
            {
                Log.Information("[NodeInstallationUtility] (Starting) Node server entry: {@serverPath}", serverPath);
                StringBuilder errorMessage = new StringBuilder();
                process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    if (e.Data != null)
                    {
                        errorMessage.AppendLine(e.Data.ToString());
                    }
                    else
                    {
                        // It seems we get false positives sometimes. Double checking if there is a message.
                        if(errorMessage.ToString().Trim().Length > 0)
                        {
                            Log.Error("[NodeInstallationUtility] Got an error from Node server on startup {@errorMessage}", errorMessage.ToString());
                        }
                    }
                };
                process.StartInfo.RedirectStandardError = true;
                process.Start();
                process.BeginErrorReadLine();
                process.StartInfo.RedirectStandardError = false;

            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                Log.Error("[NodeInstallationUtility] Error when starting Node server {@errorMessage}", errorMessage);
            }

            AppDomain.CurrentDomain.ProcessExit += async (sender, e) =>
            {
                process.Kill();
                await process.WaitForExitAsync();
            };



            FileSystemWatcher watcher = new FileSystemWatcher();
            string nodeFolderName = GetNodeFolderName();
            string envPath = @Path.Combine(GetProgramEntryPoint(), nodeFolderName, ".env");
            string? path = Path.GetDirectoryName(envPath);

            watcher.Path = String.IsNullOrEmpty(path) ? "" : path;
            watcher.Filter = Path.GetFileName(envPath);
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.EnableRaisingEvents = true;

            // Add an event handler for the file changed event

            watcher.Changed += async (sender, e) =>
            {
                try
                {
                    watcher.EnableRaisingEvents = false;
                    Log.Information("[NodeInstallationUtility] Changes found in .env! Restarting Node process...");
                    process.Kill();
                    await process.WaitForExitAsync();
                    process.Close();
                    process.Start();
                    Log.Information("[NodeInstallationUtility] Node process restarted successfully.");
                }
                finally
                {
                    watcher.EnableRaisingEvents = true;
                }
            };

        }

        public async Task StartNodeServer()
        {
            if (ConfigurationUtility.GetSectionItem("NodeHost:Enabled") != "true")
            {
                Log.Information("[NodeInstallationUtility] NodeHost:Enabled is falsy. Will *not* host any Node process within the application.");
                return;
            }

            string nodeFolder = GetNodeFolderName();
            Log.Information("[NodeInstallationUtility] NodeHost:Enabled is set to true. Will try to host the Node application in {@nodeFolder}", nodeFolder);

            if (!SufficientNodeVersionIsInstalled())
            {
                string requiredNodeVersion = ConfigurationUtility.GetSectionItem("NodeHost:MinimumVersion");
                Log.Information("[NodeInstallationUtility] Tried to host the Node application in {@nodeFolder}, but the required Node version does not seem to be installed on the system. Installed version must be at least {@requiredNodeVersion}.", nodeFolder, requiredNodeVersion);
                throw new Exception("[NodeInstallationUtility] Sufficient Node Version is not installed on the system.");
            }

            if (ConfigurationUtility.GetSectionItem("NodeHost:InstallNodeModules") == "true" && !NodeModulesExists())
            {
                await InstallNodeModules();
            }
            else
            {
                Log.Information("[NodeInstallationUtility] node_modules are already present in {@nodeFolder}, or NodeHost:InstallNodeModules is falsy. Will not install node_modules. Check the app-settings, remove the modules, and restart the application if you want a fresh install.", nodeFolder);
            }

            SetupNodeProcessAndListeners();
        }
    }
}
