using System;
using System.IO;
using System.ComponentModel;
using System.Text.RegularExpressions;
using LibUsbDotNet;
using DeviceProgramming.FileFormat;

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
        VERSION_MISMATCH,
        CONNECTION_FAILURE,
        INVALID_DFU_PROTOCOL,
        UNEXPECTED_FAILURE,
    }

    public class Aspen
    {
        private static int VendorId = 0x0483;
        private static int ProductId = 0xa367;

        private static Regex DfuVersionRegex = new Regex
            (@"(\d+)\.(\d+)\.dfu$", RegexOptions.Compiled);

        private static Regex DfuFileRegex = new Regex
            (@"\.dfu$", RegexOptions.Compiled);

        private static Regex UsbIdRegex = new Regex
            (@"^(?<vid>[a-fA-F0-9]{1,4}):(?<pid>[a-fA-F0-9]{1,4})$", RegexOptions.Compiled);

        private static Regex VersionRegex = new Regex
            (@"^(?<major>[0-9]{1,2})\.(?<minor>[0-9]{1,2})$", RegexOptions.Compiled);

        /**
         * The connected USB device to operate against.
         */
        private DeviceProgramming.Dfu.Device dfuDevice;

        /**
         * Instantiate the Aspen service and connect to the open device.
         */
        public Aspen()
        {
        }

        /**
         * This constructor should generally only be used by the test
         * environment.
         */
        public Aspen(Device dfuDevice)
        {
            this.dfuDevice = dfuDevice;
        }

        private DeviceProgramming.Dfu.Device getDfuDevice()
        {
            if (this.dfuDevice == null)
            {
                this.dfuDevice = Device.OpenFirst(UsbDevice.AllDevices, VendorId, ProductId);
            }
            return this.dfuDevice;
        }

        private bool isDfuFile(string dfuFilePath)
        {
            return DfuFileRegex.IsMatch(dfuFilePath);
        }

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
        private Version GetConnectedAspenVersion()
        {
            var device = this.getDfuDevice();
            if (device.InAppMode())
            {
                return this.getDfuDevice().Info.ProductVersion;
            }
            return new Version(0x00, 0x00);
        }

        /**
         * Get the version information from the provided DFU file. If the file
         * is invalid, return zeros for the version information.
         */
        public Version GetFirmwareVersionFromDfu(string dfuFilePath)
        {
            return GetVersionFromFileName(dfuFilePath);
            // TODO(lbayes): Get the DFU version from the file contents instead.
            // return Dfu.ParseFile(dfuFilePath).DeviceInfo.ProductVersion;
        }

        /**
         * Check all appropriate values to ensure an update can and should take
         * place. Return a helpful status code if a firmware update is not
         * appropriate at this time.
         */
        public DfuResponse ShouldUpdateFirmware(string dfuFilePath)
        {
            if (!File.Exists(dfuFilePath))
            {
                return DfuResponse.FILE_NOT_FOUND;
            }
            if (!isDfuFile(dfuFilePath))
            {
                return DfuResponse.FILE_NOT_VALID;
            }

            if (GetConnectedAspenVersion() >= GetFirmwareVersionFromDfu(dfuFilePath))
            {
                return DfuResponse.VERSION_MISMATCH;
            }

            var dfuFileData = Dfu.ParseFile(dfuFilePath);
            // Verify DFU protocol version support
            if (dfuFileData.DeviceInfo.DfuVersion != getDfuDevice().DfuDescriptor.DfuVersion)
            {
                return DfuResponse.INVALID_DFU_PROTOCOL;
            }

            return DfuResponse.SHOULD_UPDATE;
        }

        /**
         * Perform an update after verifying that it should be done. If an
         * update is not appropriate or does not success, return a helpful
         * status code.
         */
        public DfuResponse UpdateFirmware(string dfuFilePath)
        {
            DeviceProgramming.Dfu.Device device = null;

            try
            {
                var shouldResponse = ShouldUpdateFirmware(dfuFilePath);
                if (shouldResponse != DfuResponse.SHOULD_UPDATE)
                {
                    return shouldResponse;
                }

                device = this.getDfuDevice();
                Console.WriteLine("Device found in application mode, reconfiguring device to DFU mode...");
                device.Reconfigure();

                if (!device.IsOpen())
                {
                    // Clean up old device first
                    device.Dispose();
                    device = null;
                    // Reconnect to the device.
                    device = this.getDfuDevice();
                    // device = Device.OpenFirst(UsbDevice.AllDevices, vid, pid);
                    // device.DeviceError += printDevError;
                }
                else
                {
                    Console.WriteLine("Device found in DFU mode.");
                }

                var dfuFileData = Dfu.ParseFile(dfuFilePath);
                Console.WriteLine("DFU image parsed successfully.");
                int prevCursor = -1;

                EventHandler<ProgressChangedEventArgs> printDownloadProgress = (obj, e) =>
                {
                    if (prevCursor == Console.CursorTop)
                    {
                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                    }
                    Console.WriteLine("Download progress: {0}%", e.ProgressPercentage);
                    prevCursor = Console.CursorTop;
                };

                device.DownloadProgressChanged += printDownloadProgress;
                device.DownloadFirmware(dfuFileData);
                Console.WriteLine("Download successful, manifesting update...");
                device.Manifest();

                // if the device detached, clean up
                if (!device.IsOpen())
                {
                    // device.DeviceError -= printDevError;
                    device.Dispose();
                    device = null;
                }

                // TODO find device again to verify new version
                Console.WriteLine("The device has been successfully upgraded.");
                return DfuResponse.SUCCESS;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
                return DfuResponse.UNEXPECTED_FAILURE;
            }
            finally
            {
                if (device != null)
                {
                    device.Dispose();
                }
            }
        }
    }
}
