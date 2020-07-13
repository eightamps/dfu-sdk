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
                Console.WriteLine("Would you like to update now? [Y/n]");
                string response = Console.ReadLine();
                if (response == "" || response.ToUpper() == "Y")
                {
                    aspen.UpdateFirmware(path);
                }
                else
                {
                    Console.WriteLine("Deferring update for now, thank you!");
                }
            } else
            {
                Console.WriteLine("Your Aspen firmware is up to date.");
            }
        }
    }
}
