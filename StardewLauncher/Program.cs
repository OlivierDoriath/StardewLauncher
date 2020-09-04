using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace StardewLauncher
{
    static class Program
    {
        [STAThread]
        static bool Install(string source, string destination, bool? modded = null)
        {
            try
            {
                var sourceFolder = Directory.GetFiles(source, "*", SearchOption.AllDirectories);

                foreach (var s in sourceFolder)
                {
                    var sourceFile = s.Replace(source + "\\", "");
                    var destFile = Path.Combine(destination, sourceFile);
                    if ((sourceFile.StartsWith("mods", StringComparison.OrdinalIgnoreCase) ||
                        sourceFile.StartsWith("smapi", StringComparison.OrdinalIgnoreCase)) &&
                        modded == null)
                    {
                        var moddedQuestion = MessageBox.Show("The game is modded, do you want to install it along with the mods?", "Modding", MessageBoxButtons.YesNo);
                        modded = (moddedQuestion == DialogResult.Yes);
                    }
                    if ((!sourceFile.StartsWith("mods", StringComparison.OrdinalIgnoreCase) &&
                        !sourceFile.StartsWith("StardewModdingAPI", StringComparison.OrdinalIgnoreCase)) ||
                        modded == true)
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(destFile)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                        }
                        File.Copy(Path.Combine(source, sourceFile), destFile, true);
                    }
                }
            }
            catch
            {
                return false;
            }
            if (!File.Exists(Path.Combine(destination, "config.cfg")))
            {
                if (modded == null)
                {
                    modded = false;
                }
                var f = new StreamWriter(Path.Combine(destination, "config.cfg"));
                f.WriteLine(source);
                f.WriteLine(modded);
                f.Close();
            }
            return true;
        }
        [STAThread]
        static void Main()
        {
            bool ren = false;
            bool run = false;
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string currentFolder = Path.GetDirectoryName(Application.ExecutablePath);
            string steamPath;
            Version portableVersion;
            if (File.Exists(Path.Combine(currentFolder, "config.cfg")))
            {
                //Stardew Valley Portable is installed, check for Steam
                portableVersion = Version.Parse(FileVersionInfo.GetVersionInfo(Path.Combine(currentFolder, "Stardew Valley.exe")).FileVersion);
                var configFile = new StreamReader(Path.Combine(currentFolder, "config.cfg"));
                steamPath = configFile.ReadLine();
                bool modded = bool.Parse(configFile.ReadLine());
                if (Directory.Exists(steamPath))
                {
                    //We're on the home computer, check for version update
                    var installedVersion = Version.Parse(FileVersionInfo.GetVersionInfo(Path.Combine(steamPath, "Stardew Valley.exe")).FileVersion);
                    if (installedVersion.CompareTo(portableVersion) > 0)
                    {
                        //Do we want to update?
                        var result = MessageBox.Show("Your Steam version is newer than your portable version, do you want to update your portable version?", "Update?", MessageBoxButtons.YesNoCancel);
                        if (result == DialogResult.No)
                        {
                            //No update, just run the game already!
                            run = true;
                        }
                        else if (result == DialogResult.Yes)
                        {
                            //User wants to update. Let's do it!
                            run = Install(steamPath, currentFolder, modded);
                        }

                    }

                }
                else
                {
                    //We aren't home. Just run the game.
                    run = true;
                }
            }
            else
            {
                //Portable not installed. Let's do it!
                if (!File.Exists(@"C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Stardew Valley.exe"))
                {
                    //Game isn't installed in default folder. Ask the user where it is installed.
                    MessageBox.Show("Please select the game's local installation folder.");

                    var folderDialog = new FolderBrowserDialog();
                    folderDialog.ShowDialog();
                    steamPath = folderDialog.SelectedPath;
                }
                else
                {
                    steamPath = @"C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley";
                }
                if (File.Exists(Path.Combine(steamPath, "Stardew Valley.exe")))
                {
                    run = Install(steamPath, currentFolder);
                    if (run)
                    {
                        foreach (string n in FileList.dllFiles)
                        {
                            File.Copy(n, Path.Combine(currentFolder, Path.GetFileName(n)));
                        }
                    }
                }
            }
            if (run)
            {
                if (Directory.Exists(Path.Combine(appData, "StardewValley")))
                {
                    Directory.Move(Path.Combine(appData, "StardewValley"), Path.Combine(appData, "StardewValleyTemp"));
                    ren = true;
                }
                if (!Directory.Exists(Path.Combine(currentFolder, "StardewValley")))
                {
                    Directory.CreateDirectory(Path.Combine(currentFolder, "StardewValley"));
                }
                var pi = Process.Start(@"cmd.exe", @"/c mklink /D " + Path.Combine(appData, "StardewValley") + " " + Path.Combine(currentFolder, "StardewValley"));
                pi.WaitForExit();

                Process p;

                if (File.Exists(Path.Combine(currentFolder, "StardewModdingAPI.exe")))
                {
                    p = Process.Start(Path.Combine(currentFolder, "StardewModdingAPI.exe"));
                }
                else
                {
                    p = Process.Start(Path.Combine(currentFolder, "Stardew Valley.exe"));
                }

                p.WaitForExit();

                if (ren && Directory.Exists(Path.Combine(appData, "StardewValley")))
                {
                    Directory.Delete(Path.Combine(appData, "StardewValley"));
                    Directory.Move(appData + @"\StardewValleyTemp", appData + @"\StardewValley");
                }
            }
            else
            {
                //We're not running the game.
                MessageBox.Show("Launch aborted.");
            }
        }
    }
}
