using System;
using EightAmps;

namespace DfuUpdaterExample
{
    class Program
    {
        private static readonly string MapleDfuPath = @"dfu\Maple-v3.6.dfu";

        static void UpdateMaple(bool breakOnStm32 = false)
        {
            try
            {
                Console.WriteLine("------------------------");
                Console.WriteLine("UpdateMaple Now");
                var dfu = new DfuUpdater();
                dfu.BreakOnStmBootloader = breakOnStm32;
                dfu.DfuProgressChanged += OnDfuProgress;
                dfu.DfuCompleted += OnDfuCompleted;
                DfuResponse response = dfu.UpdateMapleFirmware(MapleDfuPath, false, true);
                Console.WriteLine($"Finished with {response}");
            }
            catch (Exception e)
            {
                PrintError(e);
            }
        }

        static void OnDfuProgress(int progressPercentage)
        {
            Console.WriteLine("DFU progress: {0}%", progressPercentage);
        }

        static void OnDfuCompleted(DfuResponse response)
        {
            Console.WriteLine($"DFU OnDfuCompleted with: {response}");
        }

        static void PrintError(Exception e)
        {
            Console.WriteLine("");
            Console.WriteLine("ERROR:");
            Console.WriteLine(e);
        }

        static void EnterStm32Bootloader()
        {
            UpdateMaple(true);
        }

        static void Main()
        {
            while(true)
            {
                Console.WriteLine("IMPORTANT:");
                Console.WriteLine("ENSURE device (Maple or Aspen) is powered on and connected over USB before beginning.");
                Console.WriteLine("Press m + Enter to upgrade Maple V3 firmware");
                Console.WriteLine("Press s + Enter to manually enter STM BOOTLOADER mode");
                Console.WriteLine("Press x + Enter to exit");
                string input = Console.ReadLine();
                switch (input)
                {
                    case "m":
                        UpdateMaple();
                        break;
                    case "s":
                        EnterStm32Bootloader();
                        break;
                    case "x":
                        Console.WriteLine("------------------------");
                        Console.WriteLine("Exiting now");
                        return;
                    default:
                        Console.WriteLine("------------------------");
                        Console.WriteLine("ERROR: Unexpected input, please try again.");
                        break;
                }
            }
        }
    }
}
