using System;
using System.IO;
using System.Text.RegularExpressions;
using DeviceProgramming.Dfu;
using DeviceProgramming.FileFormat;

namespace EightAmps
{
    public enum DfuResponse
    {
        SUCCESS = 0,
        VERSION_IGNORED,
        TIMEOUT = 10,
        FILE_NOT_VALID = 50,
        FILE_NOT_FOUND,
        NEWER_VERSION,
        CONNECTION_FAILURE,
        UNKNOWN_FAILURE,
    }

    public class Aspen
    {

        private static Regex DfuVersionRegex = new Regex
            (@"(\d+)\.(\d+)\.dfu$", RegexOptions.Compiled);

        private static Regex DfuFileRegex = new Regex
            (@"\.dfu$", RegexOptions.Compiled);

        private static Regex UsbIdRegex = new Regex
            (@"^(?<vid>[a-fA-F0-9]{1,4}):(?<pid>[a-fA-F0-9]{1,4})$", RegexOptions.Compiled);

        private static Regex VersionRegex = new Regex
            (@"^(?<major>[0-9]{1,2})\.(?<minor>[0-9]{1,2})$", RegexOptions.Compiled);

        public Aspen()
        {
        }

        public Aspen(Device device)
        {
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
         */
        private Version GetConnectedAspenVersion()
        {
            // TODO(lbayes): Interrogate the connected device for version info.
            return new Version(0x00, 0x00);
        }

        /**
         * Get the version information from the provided DFU file. If the file
         * is invalid, return zeros for the version information.
         */
        public Version GetFirmwareVersionFromDfu(string dfuFilePath)
        {
            if (!isDfuFile(dfuFilePath))
            {
                return new Version(0x00, 0x00);
            }

            return GetVersionFromFileName(dfuFilePath);
            // TODO(lbayes): Get the DFU version from the file contents instead.
            // return Dfu.ParseFile(dfuFilePath).DeviceInfo.ProductVersion;
        }

        /**
         * Check all appropriate values to ensure an update can and should take
         * place. Return a helpful status code if a firmware update is not
         * appropriate at this time.
         */
        private DfuResponse shouldUpdateFirmware(string dfuFilePath)
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
                return DfuResponse.NEWER_VERSION;
            }
            return DfuResponse.SUCCESS;
        }

        /**
         * Return true if all appropriate values confirm that an update can and
         * should take place. Return a helpful status code if a firmware update
         * is not appropriate at this time.
         */
        public bool ShouldUpdateFirmware(string dfuFilePath)
        {
            return shouldUpdateFirmware(dfuFilePath) == DfuResponse.SUCCESS;
        }

        /**
         * Perform an update after verifying that it should be done. If an
         * update is not appropriate or does not success, return a helpful
         * status code.
         */
        public DfuResponse UpdateFirmware(string dfuFilePath)
        {
            var shouldUpdate = shouldUpdateFirmware(dfuFilePath);
            if (shouldUpdate != DfuResponse.SUCCESS)
            {
                return shouldUpdate;
            }

            return DfuResponse.SUCCESS;
        }
    }
}
