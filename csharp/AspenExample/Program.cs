using System;
using EightAmps;

namespace AspenExample
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = "firmwares/Aspen-v1.3.dfu";
            var shouldForceVersion = false;
            string userResponse = "";
            var aspen = new Aspen();
            var shouldUpdate = aspen.ShouldUpdateFirmware(path, shouldForceVersion);
            Version version = aspen.GetFirmwareVersionFromDfu(path);
            Version oldVersion = aspen.GetConnectedAspenVersion();

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
                    DfuResponse response = aspen.UpdateFirmware(path, shouldForceVersion);
                    if (response == DfuResponse.SUCCESS)
                    {
                        Console.WriteLine("Firmware update completed successfully.");
                    }
                    else
                    {
                        Console.Write("Firmware update failed with error code {0}", response);
                    }
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
        }
    }
}
