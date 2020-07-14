using System;
using EightAmps;

namespace AspenExample
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = "firmwares/Aspen-v1.2.dfu";
            var aspen = new Aspen();
            var shouldUpdate = aspen.ShouldUpdateFirmware(path);
            if (shouldUpdate == DfuResponse.SHOULD_UPDATE)
            {
                Version version = aspen.GetFirmwareVersionFromDfu(path);
                Console.WriteLine("A firmware update (version {0}) is available.", version.ToString());
                Console.WriteLine("The computer must remain plugged in and left alone during firmware updates.");
                Console.WriteLine("Would you like to update now? [Y/n] (Press Enter for Yes)");
                string userResponse = Console.ReadLine();
                if (userResponse == "" || userResponse.ToUpper() == "Y")
                {
                    Console.WriteLine("Thank you, attempting to update firmware now.");
                    // TODO(lbayes): Subscribe to progress notifications.
                    DfuResponse response = aspen.UpdateFirmware(path);
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
            else if (shouldUpdate == DfuResponse.VERSION_IGNORED)
            {
                Console.WriteLine("Your Aspen software does not need an update at this time.");
            }
            else
            {
                Console.WriteLine("Your Aspen firmware could not be updated because: {0}", shouldUpdate);
            }
        }
    }
}
