using System;
using System.Text.RegularExpressions;
using System.Threading;
using EightAmps;

namespace DfuUpdaterExample
{
    class Program
    {
        private static readonly string MAPLE_DFU_PATH_DEFAULT = @"dfu\Maple-v3.9.dfu";

        static void UpdateMaple(bool forceUpdate = false, bool breakOnStm32 = false)
        {
            PerformUpdate(MAPLE_DFU_PATH_DEFAULT, forceUpdate, breakOnStm32);
        }

        static void UpdateMaple(string mapleDfuPath, bool forceUpdate = false, bool breakOnStm32 = false)
        {
            PerformUpdate(mapleDfuPath, forceUpdate, breakOnStm32);
        }

        static void PerformUpdate(string mapleDFUPath, bool forceUpdate = false, bool breakOnStm32 = false)
        {
            try
            {
                Console.WriteLine("------------------------");
                Console.WriteLine("UpdateMaple Now");
                var dfu = new DfuUpdater();
                dfu.BreakOnStmBootloader = breakOnStm32; // Only used for dev troubleshooting
                dfu.DfuProgressChanged += OnDfuProgress;
                dfu.DfuCompleted += OnDfuCompleted;
                DfuResponse response = dfu.UpdateMapleFirmware(mapleDFUPath, forceUpdate, true);
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
            UpdateMaple(true, true);
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine($"No arguments provided, performing force DFU update with {MAPLE_DFU_PATH_DEFAULT}");
                UpdateMaple(true);
            }
            else
            {
                var dfuPattern = new Regex(@"\.dfu$", RegexOptions.Compiled);
                var mapleDfuFilePath = args[0];

                if (!dfuPattern.IsMatch(mapleDfuFilePath))
                {
                    Console.WriteLine("Maple DFU file path is invalid, please try again (E.G. dfu\\<fileName>.dfu");
                    Environment.Exit(1);
                }
                else
                {
                    Console.WriteLine($"Maple DFU file path was provided: {mapleDfuFilePath}");
                    UpdateMaple(mapleDfuFilePath, true);
                }
            }

            Thread.Sleep(5000);

            //while(true)
            //{
            //    Console.WriteLine("------------------------");
            //    Console.WriteLine("IMPORTANT NOTE:");
            //    Console.WriteLine("Ensure device (Maple or Aspen) is powered on and connected over USB before beginning.");
            //    Console.WriteLine("------------------------");
            //    Console.WriteLine("Press [Enter] to force upgrade Maple V3 firmware (regardless of existing version)");
            //    Console.WriteLine("Provide the DFU Update Path followed by pressing [Enter]");
            //    //Console.WriteLine("Press m + Enter to upgrade Maple V3 firmware only if necessary");
            //    //Console.WriteLine("Press s + Enter to manually enter STM BOOTLOADER mode");
            //    //Console.WriteLine("Press x + Enter to exit");
            //    //Console.WriteLine("------------------------");
            //    string input = Console.ReadLine();
            //    switch (input)
            //    {
            //        case "":
            //            UpdateMaple(true);
            //            break;
            //        case "m":
            //            UpdateMaple();
            //            break;
            //        case "s":
            //            EnterStm32Bootloader();
            //            break;
            //        case "x":
            //            Console.WriteLine("------------------------");
            //            Console.WriteLine("Exiting now");
            //            return;
            //        default:
            //            Console.WriteLine("------------------------");
            //            Console.WriteLine("ERROR: Unexpected input, please try again.");
            //            break;
            //    }
            //}
        }
    }
}
