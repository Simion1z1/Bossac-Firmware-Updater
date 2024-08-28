using System;
using System.Diagnostics;
using System.IO;
using System.Net;


class Program
{

    private static readonly string versionFileUrl = "https://raw.githubusercontent.com/Simion1z1/OFG-G101-Firmware/main/firmware_version.txt";
    private static readonly string firmwareUrl = "https://raw.githubusercontent.com/Simion1z1/OFG-G101-Firmware/main/firmware.bin";
    private static readonly string programPath = AppDomain.CurrentDomain.BaseDirectory;
    private static readonly string firmwareDirectory = Path.Combine(programPath, "Firmware", "G101");
    private static readonly string localVersionFilePath = Path.Combine(firmwareDirectory, "version.txt");
    private static readonly string firmwareDownloadPath = Path.Combine(firmwareDirectory, "firmware.bin");







    static void Main(string[] args)
    {

        if (!BossacInstalledCheck())
        {
            InstallBossac();
        }

        if (IsUpdateRequired())
        {
            DownloadFirmware();
            UpdateFirmware("COM4");
           // UpdateLocalVersionFile();
        }
        else
        {
            Console.WriteLine("Firmware is up-to-date.");
        }

    }

    static bool BossacInstalledCheck() {
        // Define the expected path where bossac should be installed
        //string bossacPath = @"C:\Program Files\BOSSA\bossac.exe";
        string bossacPath = @"C:\Users\logos\.platformio\packages\tool-bossac\bossac.exe";


        // Check if bossac is installed
        if (File.Exists(bossacPath))
        {
            Console.WriteLine("bossac is already installed.");
            return true;
        }
        else
        {
            Console.WriteLine("bossac not found. Downloading and installing...");
            return false;
        }

        // Proceed with the rest of your logic
        Console.ReadLine(); // To keep the console open
 
    }

   static public void InstallBossac()
    {

    string bossacDownloadUrl = "https://github.com/shumatech/BOSSA/releases/download/1.9.1/bossa-x64-1.9.1.msi";
    string installerPath = Path.Combine(Path.GetTempPath(), "bossa-x64-1.9.1.msi");

        try
        {
            // Download the .msi installer
            using (WebClient client = new WebClient())
            {
                Console.WriteLine("Downloading bossac installer...");
                client.DownloadFile(bossacDownloadUrl, installerPath);
            }

            Console.WriteLine("Downloaded bossac installer to " + installerPath);

            // Install bossac using the .msi installer
            Process installerProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "msiexec",
                    Arguments = $"/i \"{installerPath}\" /quiet /norestart",
                    UseShellExecute = true,
                    Verb = "runas", // Run as administrator
                }
            };

            installerProcess.Start();
            installerProcess.WaitForExit();

            // Check the exit code
            if (installerProcess.ExitCode == 0)
            {
                Console.WriteLine("BOSSA installed successfully.");
            }
            else
            {
                Console.WriteLine("BOSSA installation failed with exit code: " + installerProcess.ExitCode);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to install BOSSA: " + ex.Message);
        }
        finally
        {
            // Clean up the installer file
            if (File.Exists(installerPath))
            {
                try
                {
                    File.Delete(installerPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to delete installer file: " + ex.Message);
                }
            }
        }
    }


    static public string GetOnlineFirmwareVersion()
    {
        try
        {
            using (WebClient client = new WebClient())
            {
                // Download the version file content
                string versionFileContent = client.DownloadString(versionFileUrl).Trim();

                // Extract the version number
                if (versionFileContent.StartsWith("version="))
                {
                    string onlineVersion = versionFileContent.Substring("version=".Length).Trim();
                    Console.WriteLine("Online firmware version: " + onlineVersion);
                    return onlineVersion;
                }
                else
                {
                    Console.WriteLine("Unexpected format in version file.");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error checking online firmware version: " + ex.Message);
            return null;
        }
    }

    static public string GetLocalFirmwareVersion()
    {
        try
        {
            if (File.Exists(localVersionFilePath))
            {
                // Read the local version file content
                string localVersionFileContent = File.ReadAllText(localVersionFilePath).Trim();

                // Extract the version number
                if (localVersionFileContent.StartsWith("version="))
                {
                    string localVersion = localVersionFileContent.Substring("version=".Length).Trim();
                    Console.WriteLine("Local firmware version: " + localVersion);
                    return localVersion;
                }
                else
                {
                    Console.WriteLine("Unexpected format in local version file.");
                    return null;
                }
            }
            else
            {
                Console.WriteLine("Local version file not found.");


                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading local firmware version: " + ex.Message);
            return null;
        }


    }

    static public bool IsUpdateRequired()
    {
        string onlineVersion = GetOnlineFirmwareVersion();
        string localVersion = GetLocalFirmwareVersion();

        if (onlineVersion == null)
        {
            Console.WriteLine("Could not determine the online firmware version.");
            return false;
        }

        if (localVersion == null || !onlineVersion.Equals(localVersion, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Firmware update required.");
            return true;
        }

        Console.WriteLine("No firmware update required.");
        return false;
    }

    static public void DownloadFirmware()
    {
        try
        {
            // Use WebClient to download the firmware binary
            using (WebClient client = new WebClient())
            {
                Console.WriteLine("Downloading firmware from " + firmwareUrl + "...");

                // Download the file and save it to the specified path
                client.DownloadFile(firmwareUrl, firmwareDownloadPath);
            }

            Console.WriteLine("Firmware downloaded successfully to " + firmwareDownloadPath);
        }
        catch (WebException webEx)
        {
            Console.WriteLine("Error downloading firmware: " + webEx.Message);
            Console.WriteLine("Status code: " + ((HttpWebResponse)webEx.Response)?.StatusCode);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unexpected error: " + ex.Message);
        }
    }

    static void UpdateFirmware(string comPort)
    {
        // Path to the BOSSA executable
        string bossacPath = @"C:\Users\logos\.platformio\packages\tool-bossac\bossac.exe";
        string customFirmwarePath = @"D:\OFG\Proiect OFG x HardwareMonitor\Vs code\OFG - Vs code\Xiao Seeeduino\.pio\build\seeed_xiao\firmware.bin";

        // Ensure COM port is provided
        if (string.IsNullOrWhiteSpace(comPort))
        {
            Console.WriteLine("COM port must be specified.");
            return;
        }

        // Arguments for flashing the firmware with the specified COM port
        string arguments = $"-p {comPort} -e -w -v -b \"{firmwareDownloadPath}\" -R";

        try
        {
            // Check if BOSSA exists
            if (!File.Exists(bossacPath))
            {
                Console.WriteLine("BOSSA executable not found at " + bossacPath);
                return;
            }

            Console.WriteLine("Executing command: " + bossacPath + " " + arguments);

            // Set up the process to run BOSSA
            using (Process flashProcess = new Process())
            {
                flashProcess.StartInfo.FileName = bossacPath;
                flashProcess.StartInfo.Arguments = arguments;
                flashProcess.StartInfo.UseShellExecute = false;
                flashProcess.StartInfo.RedirectStandardOutput = true;
                flashProcess.StartInfo.RedirectStandardError = true;
                flashProcess.StartInfo.CreateNoWindow = true;

                // Start the process
                flashProcess.Start();

                // Read the output and errors
                string output = flashProcess.StandardOutput.ReadToEnd();
                string error = flashProcess.StandardError.ReadToEnd();
                flashProcess.WaitForExit();

                // Display output and errors
                Console.WriteLine("BOSSA output: " + output);
                if (flashProcess.ExitCode == 0)
                {
                    Console.WriteLine("Firmware updated successfully.");
                }
                else
                {
                    Console.WriteLine("Firmware update failed with exit code: " + flashProcess.ExitCode);
                    Console.WriteLine("BOSSA error: " + error);
                }
            }

            // Wait for a moment to ensure the board has time to reset
            Console.WriteLine("Waiting for the board to reset...");
            System.Threading.Thread.Sleep(5000); // 5 seconds wait; adjust if necessary

            // Optionally: Add code to verify if the board has started correctly (e.g., through serial communication)
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error updating firmware: " + ex.Message);
        }
    }

}
