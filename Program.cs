using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Management;


class Program
{

    private static readonly string versionFileUrl = "https://raw.githubusercontent.com/Simion1z1/OFG-G101-Firmware/main/firmware_version.txt";
    private static readonly string firmwareUrl = "https://raw.githubusercontent.com/Simion1z1/OFG-G101-Firmware/main/firmware.bin";
    private static readonly string programPath = AppDomain.CurrentDomain.BaseDirectory;
    private static readonly string firmwareDirectory = Path.Combine(programPath, "Firmware", "G101");
    private static readonly string localVersionFilePath = Path.Combine(firmwareDirectory, "version.txt");
    private static readonly string firmwareDownloadPath = Path.Combine(firmwareDirectory, "firmware.bin");

    public static string DetectedPort { get; private set; }


    static async Task Main(string[] args)
    {

        // string newFirmwareVersion = GetOnlineFirmwareVersion(); //uncomment this if you use manual function / GetOnlineFirmwareVersion()  //if I uncomment, it shows me the firmware version, local and uploaded, in cmd
        /*
         string detectedPort = DetectNewComPort(); // test the COMPort Connections
         if (!string.IsNullOrEmpty(detectedPort))
         {
             Console.WriteLine("Detected new COM port: " + detectedPort);
             // You can now proceed with your firmware update process using detectedPort
         }
         else
         {
             Console.WriteLine("No new COM port detected.");
         }*/

        if (!BossacInstalledCheck())
            {
                InstallBossac();
            }

            if (IsUpdateRequired())
            {
                DownloadFirmware();

            //Check Port Automatically and upload the firmware
            Console.WriteLine("Waiting for device connection...");
            Console.WriteLine("Please enter in Bootloader Mode");

            // Start detecting the new COM port automatically
            await DetectNewComPortAutomatically();

            // Now you can use the DetectedPort variable
            if (!string.IsNullOrEmpty(DetectedPort))
            {
                Console.WriteLine("Detected new COM port: " + DetectedPort);
                // You can now proceed with your firmware update process using DetectedPort
            }
            else
            {
                Console.WriteLine("No new COM port detected.");
            }



            UpdateFirmware(DetectedPort); // IN BRACKETS PLEASE ADD COM PORT!!!!!! THE PORT AFTER ENTERED TO BOOTLOADER MODE!!!!!!!!!
                if (!string.IsNullOrEmpty(GetOnlineFirmwareVersion()))
                {
                    UpdateLocalVersionFile(GetOnlineFirmwareVersion());
                }
                else
                {
                    Console.WriteLine("Failed to retrieve the new firmware version.");
                }

            }
            else
            {
                Console.WriteLine("Firmware is up-to-date.");
            }

    }

    static bool BossacInstalledCheck() {
        // Define the expected path where bossac should be installed
        //string bossacPath = @"C:\Program Files\BOSSA\bossac.exe";
        string bossacPath = @"C:\Users\logos\.platformio\packages\tool-bossac\bossac.exe";  //this is for a test


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
        //string customFirmwarePath = @"D:\OFG\Proiect OFG x HardwareMonitor\Vs code\OFG - Vs code\Xiao Seeeduino\.pio\build\seeed_xiao\firmware.bin"; //this is for a test
        
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

    static void UpdateLocalVersionFile(string newVersion)
{
    try
    {
        // Ensure the directory exists
        if (!Directory.Exists(firmwareDirectory))
        {
            Directory.CreateDirectory(firmwareDirectory);
        }

        // Write the new version to the local version file
        File.WriteAllText(localVersionFilePath, $"version={newVersion}");

        Console.WriteLine("Local version file updated to: " + newVersion);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error updating local version file: " + ex.Message);
    }
}

    /*
    static string DetectNewComPort()
    {
        // Get the initial list of COM ports
        string[] initialPorts = SerialPort.GetPortNames();

        Console.WriteLine("Please plug in your hardware and press Enter...");
        Console.ReadLine();

        // Wait for the user to plug in the device and the system to recognize it
        Thread.Sleep(500); // You can adjust this delay if needed

        // Get the updated list of COM ports
        string[] updatedPorts = SerialPort.GetPortNames();

        // Identify the new COM port
        string newPort = updatedPorts.Except(initialPorts).FirstOrDefault();

        return newPort;
    } */ //Version with enter hit (not automatically detected when entered when is reseted/entered to bootloader


    /*static void DetectNewComPortAutomatically()
     {
        // Get initial list of COM ports
        string[] initialPorts = SerialPort.GetPortNames();

        // Setup event watcher for when a new COM port is connected
        var watcher = new ManagementEventWatcher();
        watcher.Query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");
        watcher.EventArrived += (sender, eventArgs) =>
        {
            // Get the updated list of COM ports
            string[] updatedPorts = SerialPort.GetPortNames();

            // Identify the new COM port
            string newPort = updatedPorts.Except(initialPorts).FirstOrDefault();

            if (!string.IsNullOrEmpty(newPort))
            {
                Console.WriteLine("Detected new COM port: " + newPort);

                // You can now proceed with your firmware update process using newPort

                // Optionally stop listening after detecting the first port
                //watcher.Stop();

            }
        };

        // Start listening for events
        watcher.Start();

        // Keep the application running to listen for the event
        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    } */

    static Task DetectNewComPortAutomatically()
    {
        // Use TaskCompletionSource to handle asynchronous operation
        var tcs = new TaskCompletionSource<bool>();

        // Get the initial list of COM ports
        string[] initialPorts = SerialPort.GetPortNames();

        // Setup event watcher for when a new COM port is connected
        var watcher = new ManagementEventWatcher();
        watcher.Query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");

        watcher.EventArrived += (sender, eventArgs) =>
        {
            // Get the updated list of COM ports
            string[] updatedPorts = SerialPort.GetPortNames();

            // Identify the new COM port
            string newPort = updatedPorts.Except(initialPorts).FirstOrDefault();

            if (!string.IsNullOrEmpty(newPort))
            {
                // Store the new COM port in the public variable
                DetectedPort = newPort;

                tcs.SetResult(true);  // Signal that the task is complete

                // Optionally stop listening after detecting the first port
                watcher.Stop();
            }
        };

        // Start listening for events
        watcher.Start();

        // Return the task that will complete when a new port is detected
        return tcs.Task;
    }
}


