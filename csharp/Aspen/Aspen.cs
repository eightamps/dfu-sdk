using System;
using System.IO;
using System.ComponentModel;
using System.Text.RegularExpressions;
using LibUsbDotNet;
using DeviceProgramming.FileFormat;
using System.Threading;

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
        VERSION_IS_OKAY,
        CONNECTION_FAILURE,
        INVALID_DFU_PROTOCOL,
        UNEXPECTED_FAILURE,
    }

    public class Aspen : IAspen
    {
        private static int VendorId = 0x0483;
        private static int ProductId = 0xa367;

        private static Regex DfuVersionRegex = new Regex
            (@"(\d+)\.(\d+)\.dfu$", RegexOptions.Compiled);

        private static Regex DfuFileRegex = new Regex
            (@"\.dfu$", RegexOptions.Compiled);

        /**
         * The connected USB device to operate against.
         */
        private DeviceProgramming.Dfu.Device dfuDevice;
        private int prevCursor = -1;

        public bool IsUpdating => throw new NotImplementedException();

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
        public Aspen(DeviceProgramming.Dfu.Device dfuDevice)
        {
            this.dfuDevice = dfuDevice;
        }

        /**
         * Event fired when DFU download proceeds.
         */
        public event Action<int> DownloadProgressChanged;

        public event Action<DfuResponse> DownloadCompleted;

        /**
         * Event fired when a device error is detected.
         */
        public event EventHandler<ErrorEventArgs> DeviceError;

        private void DownloadProgressChangedHandler(object obj, ProgressChangedEventArgs e)
        {
            this.DownloadProgressChanged?.Invoke(e.ProgressPercentage);
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
        private DeviceProgramming.Dfu.Device getOrCreateDevice()
        {
            if (this.dfuDevice == null)
            {
                try
                {
                    this.dfuDevice = this.CreateDevice();
                }
                catch { }
            }
            return this.dfuDevice;
        }

        private DeviceProgramming.Dfu.Device CreateDevice()
        {
            var device = Device.OpenFirst(UsbDevice.AllDevices, VendorId, ProductId);
            device.DeviceError += DeviceErrorHandler;
            device.DownloadProgressChanged += DownloadProgressChangedHandler;
            return device;
        }

        private void ClearDevice()
        {
            if (this.dfuDevice != null)
            {
                this.dfuDevice.DeviceError -= DeviceErrorHandler;
                this.dfuDevice.DownloadProgressChanged -= DownloadProgressChangedHandler;
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
        private bool isDfuFile(string dfuFilePath)
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
            var device = this.getOrCreateDevice();
            if (device.InAppMode())
            {
                return this.getOrCreateDevice().Info.ProductVersion;
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
        public DfuResponse ShouldUpdateFirmware(string dfuFilePath, bool forceVersion = false)
        {
            if (!File.Exists(dfuFilePath))
            {
                return DfuResponse.FILE_NOT_FOUND;
            }
            if (!isDfuFile(dfuFilePath))
            {
                return DfuResponse.FILE_NOT_VALID;
            }

            if (!forceVersion && GetConnectedAspenVersion() >= GetFirmwareVersionFromDfu(dfuFilePath))
            {
                return DfuResponse.VERSION_IS_OKAY;
            }

            var dfuFileData = Dfu.ParseFile(dfuFilePath);
            // Verify DFU protocol version support
            var aspenBoard = this.getOrCreateDevice();

            if(aspenBoard == null)
            {
                return DfuResponse.CONNECTION_FAILURE;
            }

            aspenBoard = getOrCreateDevice();

            if (aspenBoard != null && dfuFileData.DeviceInfo.DfuVersion != aspenBoard.DfuDescriptor.DfuVersion)
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
        public void UpdateFirmware(string dfuFilePath, bool forceVersion = false)
        {
            DeviceProgramming.Dfu.Device device = null;

            try
            {
                var shouldResponse = ShouldUpdateFirmware(dfuFilePath, forceVersion);
                if (shouldResponse != DfuResponse.SHOULD_UPDATE)
                {
                    DownloadCompleted?.Invoke(shouldResponse);
                    return;
                }

                device = this.getOrCreateDevice();
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

                device = this.getOrCreateDevice();
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
                DownloadCompleted?.Invoke(DfuResponse.SUCCESS);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
                DownloadCompleted?.Invoke(DfuResponse.UNEXPECTED_FAILURE);
            }
            finally
            {
                if (device != null)
                {
                    ClearDevice();
                }
            }
        }
    }
}
