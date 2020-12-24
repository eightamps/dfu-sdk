using System;
using System.IO;
using System.ComponentModel;
using System.Text.RegularExpressions;
using LibUsbDotNet;
using DeviceProgramming.FileFormat;
using System.Threading;
using static DeviceProgramming.FileFormat.Dfu;
using System.Diagnostics;
using EightAmps.utils;

namespace EightAmps
{

    public enum DfuResponse
    {
        SUCCESS = 0,
        SHOULD_UPDATE,
        VERSION_IGNORED,
        TIMEOUT = 10,
        FILE_NOT_VALID = 50,
        FILE_NOT_FOUND,
        DEVICE_NOT_FOUND,
        STM32_BOOTLOADER_NOT_FOUND,
        BROKE_ON_STM32,
        VERSION_IS_OKAY,
        CONNECTION_FAILURE,
        INVALID_DFU_PROTOCOL,
        UNEXPECTED_FAILURE,
    }

    public class DfuUpdater : IDfuUpdater
    {
        public static readonly int MapleVendorId = 0x335e;
        public static readonly int MapleProductId = 0x8a01;
        public static readonly string MapleUpdateArgs = $"--vid {MapleVendorId} --pid {MapleProductId} -b --iid 0 --name \"Maple Bootloader\" --dest \"maple3_driver\"";

        public static readonly int STM32VendorId = 0x0483;
        public static readonly int STM32ProductId = 0xDF11;
        public static readonly string STM32UpdateArgs = $"--vid {STM32VendorId} --pid {STM32ProductId} -b --name \"STM BOOTLOADER\" --dest \"stm32_driver\"";

        public static readonly int AspenVendorId = 0x0483;
        public static readonly int AspenProductId = 0xa367;
        public static readonly string AspenUpdateArgs = $"--vid {AspenVendorId} --pid {AspenProductId} -b --iid 0 --name \"Aspen Bootloader\" --dest \"aspen_driver\"";

        public static readonly int Aspen3VendorId = 0x335e;
        public static readonly int Aspen3ProductId = 0x8a10;
        public static readonly string Aspen3UpdateArgs = $"--vid {Aspen3VendorId} --pid {Aspen3ProductId} -b --iid 0 --name \"Aspen Bootloader\" --dest \"aspen3_driver\"";

        public static readonly string FirmwareUpdateExe = @"wdi\wdi-simple.exe";

        private static readonly Regex DfuVersionRegex = new Regex
            (@"(\d+)\.(\d+)\.dfu$", RegexOptions.Compiled);

        private static readonly Regex DfuFileRegex = new Regex
            (@"\.dfu$", RegexOptions.Compiled);

        /**
         * The connected USB device to operate against.
         */
        private DeviceProgramming.Dfu.Device dfuDevice;

        /**
         * Used for troubleshooting only. Setting this flag to true will
         * cause a request for update to end when we've entered the
         * STM32 Bootloader
         */
        public bool BreakOnStmBootloader;

        /**
         * Instantiate the Aspen service and connect to the open device.
         */
        public DfuUpdater()
        {
        }

        /**
         * This constructor should generally only be used by the test
         * environment.
         */
        public DfuUpdater(DeviceProgramming.Dfu.Device dfuDevice)
        {
            this.dfuDevice = dfuDevice;
        }

        /**
         * Event fired when DFU download proceeds.
         */
        public event Action<int> DfuProgressChanged;

        public event Action<DfuResponse> DfuCompleted;

        /**
         * Event fired when a device error is detected.
         */
        public event EventHandler<ErrorEventArgs> DeviceError;

        private void DfuProgressChangedHandler(object obj, ProgressChangedEventArgs e)
        {
            this.DfuProgressChanged?.Invoke(e.ProgressPercentage);
        }

        private void DeviceErrorHandler(object obj, ErrorEventArgs e)
        {
            var exception = e.GetException();
            // NOTE(lbayes): Getting this random error on the event bus.
            if (exception.Message != "Firmware")
            {
                this.DeviceError?.Invoke(this, e);
            }
        }

        /**
         * Get the connected DFU Device or establish a connection if one hasn't
         * already been made.
         */
        private DeviceProgramming.Dfu.Device GetOrCreateDevice(int vid, int pid)
        {
            if (this.dfuDevice == null)
            {
                try
                {
                    this.dfuDevice = this.CreateDevice(vid, pid);
                }
                catch { }
            }

            return this.dfuDevice;
        }

        private DeviceProgramming.Dfu.Device CreateDevice(int vid, int pid)
        {
            var device = Device.OpenFirst(UsbDevice.AllDevices, vid, pid);
            device.DeviceError += DeviceErrorHandler;
            device.DownloadProgressChanged += DfuProgressChangedHandler;
            return device;
        }

        private void ClearDevice()
        {
            if (this.dfuDevice != null)
            {
                this.dfuDevice.DeviceError -= DeviceErrorHandler;
                this.dfuDevice.DownloadProgressChanged -= DfuProgressChangedHandler;
                if (this.dfuDevice is IDisposable)
                {
                    ((IDisposable)this.dfuDevice).Dispose();
                }
            }
            this.dfuDevice = null;
            Thread.Sleep(500);
        }

        /**
         * Return true if the provided file name looks like a DFU file.
         */
        private bool IsDfuFile(string dfuFilePath)
        {
            return DfuFileRegex.IsMatch(dfuFilePath);
        }

        /**
         * Extract the version number from a DFU file.
         */
        private Version GetVersionFromFileName(string dfuFilePath)
        {
            var matched = DfuVersionRegex.Match(dfuFilePath);
            var major = Int32.Parse(matched.Groups[1].ToString());
            var minor = Int32.Parse(matched.Groups[2].ToString());
            return new Version(major, minor);
        }

        /**
         * Interrogate the connected USB device and find out what version it
         * was flashed with.
         * If there is no device connected, or the device is not in
         * Appplication mode, return a zero version to allow progress.
         */
        public Version GetConnectedAspenVersion()
        {
            return this.GetConnectedVersion(AspenVendorId, AspenProductId);
        }

        public Version GetConnectedMapleVersion()
        {
            return GetConnectedVersion(AspenVendorId, AspenProductId);
        }
        public Version GetConnectedVersion(int vid, int pid)
        {
            var device = this.GetOrCreateDevice(vid, pid);

            if (device != null)
            {
                if (device.InAppMode())
                {
                    return device.Info.ProductVersion;
                }
            }

            return new Version(0x00, 0x00);
        }
        public DfuResponse UpdateAspenFirmware(string dfuFilePath, bool forceVersion = false, bool installDriver = true)
        {
            return UpdateFirmware(dfuFilePath, AspenVendorId, AspenProductId, forceVersion, installDriver);
        }

        public DfuResponse UpdateMapleFirmware(string dfuFilePath, bool forceVersion = false, bool installDriver = true)
        {
            return UpdateFirmware(dfuFilePath, MapleVendorId, MapleProductId, forceVersion, installDriver);
        }

        /**
         * Get the version information from the provided DFU file. If the file
         * is invalid, return zeros for the version information.
         */
        public Version GetFirmwareVersionFromDfu(string dfuFilePath)
        {
            return GetVersionFromFileName(dfuFilePath);
            // NOTE(lbayes): Parsed firmware version returns Version(255, 255);
            // Figure out if we have a bug in our firmware package, or the parser.
            // return Dfu.ParseFile(dfuFilePath).DeviceInfo.ProductVersion;
        }

        /**
         * Check all appropriate values to ensure an update can and should take
         * place. Return a helpful status code if a firmware update is not
         * appropriate at this time.
         */
        public DfuResponse ShouldUpdateFirmware(string dfuFilePath, DeviceProgramming.Dfu.Device device, bool forceVersion = false, bool shouldUpdate = true)
        {
            Version connectedVersion = new Version(0, 0);
            Version firmwareVersion;

            if (!File.Exists(dfuFilePath))
            {
                return DfuResponse.FILE_NOT_FOUND;
            }

            if (!IsDfuFile(dfuFilePath))
            {
                return DfuResponse.FILE_NOT_VALID;
            }

            firmwareVersion = GetFirmwareVersionFromDfu(dfuFilePath);

            FileContent dfuFileData = Dfu.ParseFile(dfuFilePath);
            if (dfuFileData.DeviceInfo.DfuVersion != device.DfuDescriptor.DfuVersion)
            {
                return DfuResponse.INVALID_DFU_PROTOCOL;
            }

            if (device.InAppMode())
            {
                connectedVersion = device.Info.ProductVersion;
            }

            if (!forceVersion && connectedVersion >= firmwareVersion)
            {
                return DfuResponse.VERSION_IS_OKAY;
            }

            return DfuResponse.SHOULD_UPDATE;
        }

        private DfuResponse ContinueWithStmBootloader(string dfuFilePath, bool installDriver = true)
        {
            try
            {
                this.ClearDevice();
                DeviceProgramming.Dfu.Device device = this.GetOrCreateDevice(STM32VendorId, STM32ProductId);

                if (device == null)
                {
                    if (!installDriver)
                    {
                        return DfuResponse.STM32_BOOTLOADER_NOT_FOUND;
                    }
                    InstallDriversFor(STM32VendorId, STM32ProductId);
                    return ContinueWithStmBootloader(dfuFilePath, false);
                }

                var dfuFileData = Dfu.ParseFile(dfuFilePath);
                Console.WriteLine("DFU image parsed successfully.");
                device.DownloadFirmware(dfuFileData);
                Console.WriteLine("Download successful, manifesting update...");
                device.Manifest();

                // if the device detached, clean up
                if (!device.IsOpen())
                {
                    ClearDevice();
                }

                // TODO find device again to verify new version
                Console.WriteLine("The device has been successfully upgraded.");
                DfuCompleted?.Invoke(DfuResponse.SUCCESS);
                return DfuResponse.SUCCESS;
            }
            finally
            {
                ClearDevice();
            }
        }

        /**
         * Perform an update after verifying that it should be done. If an
         * update is not appropriate or does not success, return a helpful
         * status code.
         */
        public DfuResponse UpdateFirmware(string dfuFilePath, int vid, int pid, bool forceVersion = false, bool installDriver = false)
        {
            try
            {
                DeviceProgramming.Dfu.Device device = GetOrCreateDevice(vid, pid);
                if (device == null)
                {
                    if (AlreadyInStmBootloader())
                    {
                        ClearDevice();
                        return ContinueWithStmBootloader(dfuFilePath);
                    }
                    if (!installDriver)
                    {
                        return DfuResponse.DEVICE_NOT_FOUND;
                    }
                    InstallDriversFor(vid, pid);
                    return UpdateFirmware(dfuFilePath, vid, pid, forceVersion, false);
                }

                var shouldResponse = ShouldUpdateFirmware(dfuFilePath, device, forceVersion);
                if (shouldResponse != DfuResponse.SHOULD_UPDATE)
                {
                    DfuCompleted?.Invoke(shouldResponse);
                    return shouldResponse;
                }

                Console.WriteLine("Device found in application mode, reconfiguring device to DFU mode...");
                device.Reconfigure();

                if (!device.IsOpen())
                {
                    ClearDevice();
                }
                else
                {
                    Console.WriteLine("Device found in DFU mode.");
                }

                if (BreakOnStmBootloader)
                {
                    return DfuResponse.BROKE_ON_STM32;
                }

                return ContinueWithStmBootloader(dfuFilePath);
            }
            finally
            {
                ClearDevice();
            }
        }

        private bool AlreadyInStmBootloader()
        {
            var device = GetOrCreateDevice(STM32VendorId, STM32ProductId);
            ClearDevice(); // Make sure we don't mess up the device cache when checking this.
            return device != null;
        }

        private string GetInfName(int vid, int pid)
        {
            if (vid == STM32VendorId && pid == STM32ProductId)
            {
                return "STM32_Bootloader.inf";
            }

            return "usb_device.inf";
        }

        private string GetDeviceDriverArgs(int vid, int pid)
        {
            if (vid == STM32VendorId && pid == STM32ProductId)
            {
                return STM32UpdateArgs;
            }
            else if (vid == MapleVendorId && pid == MapleProductId)
            {
                return MapleUpdateArgs;
            }
            else if (vid == AspenVendorId && pid == AspenProductId)
            {
                return AspenUpdateArgs;
            }
            else if (vid == Aspen3VendorId && pid == Aspen3ProductId)
            {
                return Aspen3UpdateArgs;
            }

            return null;
        }

        public void InstallDriversFor(int vid, int pid)
        {
            Console.WriteLine("");
            Console.WriteLine("----------------------------------");
            Console.WriteLine($"Install WinUSB Driver for vid:0x{vid:X} pid:0x{pid:X}");
            string args = GetDeviceDriverArgs(vid, pid);
            Console.WriteLine($"Executing: {FirmwareUpdateExe} {args}");
            Process process = ProcessHelper.GetProcess(FirmwareUpdateExe, args, OnProcessData, OnProcessError);
            try
            {
                Console.WriteLine("Driver installation may take several minutes");
                Console.WriteLine("DO NOT EXIT");
                process.Start();
                process.WaitForExit();
                Console.WriteLine("Finished installing driver");
            }
            catch (Win32Exception e)
            {
                Console.WriteLine("Driver install not permitted, you must run this application as administrator.");
                Console.WriteLine(e.ToString());
            }
        }

        void OnProcessData(object sender, DataReceivedEventArgs args)
        {
            Console.WriteLine("OnDataReceived: " + args.Data.ToString());
        }

        void OnProcessError(object sender, DataReceivedEventArgs args)
        {
            Console.WriteLine("OnErrorReceived: " + args.Data.ToString());
        }
    }
}
