using System;
using System.ComponentModel;
using System.Net;
using System.Threading;
using EightAmps;

namespace AspenExample
{
    class Program
    {
        static void Main()
        {
            while (true) {
                // string path = "firmwares/Aspen-v1.2.dfu";
                var shouldForceVersion = false;
                string userResponse = "";
                Console.WriteLine("Provide a path to a firmware update (e.g., C:\\Aspen-v1.1.dfu), or type 'q' to quit:");
                string path = Console.ReadLine();

                if (path == "")
                {
                    Console.WriteLine("Path must not be empty.");
                    continue;
                }

                if (path == "q")
                {
                    Console.WriteLine("Exiting Now");
                    Thread.Sleep(500);
                    System.Environment.Exit(0);
                }

                Console.WriteLine("Performing update with {0}", path);

                var aspen = new Aspen();
                var shouldUpdate = aspen.ShouldUpdateFirmware(path, shouldForceVersion);
                Version version = aspen.GetFirmwareVersionFromDfu(path);
                Version oldVersion = null;
                try
                {
                    oldVersion = aspen.GetConnectedAspenVersion();
                } catch (Exception e)
                {
                    Console.WriteLine("No device found");
                    Console.WriteLine(e.ToString());
                }

                aspen.DownloadProgressChanged += (int progressPercentage) =>
                {
                    Console.WriteLine("Download progress: {0}%", progressPercentage);
                };

                if (shouldUpdate == DfuResponse.VERSION_IS_OKAY)
                {
                    Console.WriteLine("A firmware update version {0} is available and you have version {1} installed.",
                        version.ToString(), oldVersion.ToString());
                    Console.WriteLine("Would you like to force the update anyway? [Y/n]");
                    userResponse = Console.ReadLine();
                    if (userResponse == "" || userResponse.ToUpper() == "Y")
                    {
                        shouldForceVersion = true;
                    }
                }

                if (shouldForceVersion || shouldUpdate == DfuResponse.SHOULD_UPDATE)
                {
                    Console.WriteLine("The computer must remain plugged in and left alone during firmware updates.");
                    if (!shouldForceVersion)
                    {
                        Console.WriteLine("A firmware update version {0} is available and you have version {1} installed.",
                            version.ToString(), oldVersion.ToString());
                        Console.WriteLine("Would you like to update now? [Y/n] (Press Enter for Yes)");
                        userResponse = Console.ReadLine();
                    }

                    if (userResponse == "" || userResponse.ToUpper() == "Y")
                    {
                        Console.WriteLine("Thank you, attempting to update firmware now.");
                        // TODO(lbayes): Subscribe to progress notifications.
                        aspen.UpdateFirmware(path, shouldForceVersion);
                        /*
                        if (response == DfuResponse.SUCCESS)
                        {
                            Console.WriteLine("Firmware update completed successfully.");
                        }
                        else
                        {
                            Console.Write("Firmware update failed with error code {0}", response);
                        }
                        */
                    }
                    else
                    {
                        Console.WriteLine("Deferring update for now, thank you!");
                    }
                }
                else
                {
                    Console.WriteLine("Your Aspen firmware could not be updated because: {0}", shouldUpdate);
                }

                Console.WriteLine("Press 'q' followed by 'enter' to exit, or just 'enter' to try again.");
                var shouldExit = Console.ReadLine();
                if (shouldExit == "q")
                {
                    Console.WriteLine("Exiting now");
                    Thread.Sleep(500);
                    System.Environment.Exit(0);
                }
            }
        }
    }
}
