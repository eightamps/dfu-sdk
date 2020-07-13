using System;
using EightAmps;

namespace AspenExample
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = "firmwares/one.dfu";
            var aspen = new Aspen();
            if (aspen.ShouldUpdateFirmware(path))
            {
                Version version = aspen.GetFirmwareVersion(path);
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
            else
            {
                Console.WriteLine("Your Aspen firmware is already up to date.");
            }
        }
    }
}
