using System;
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
        NOT_DFU_FILE = 50,
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

        public Version GetFirmwareVersionFromDfu(string dfuFilePath)
        {
            if (!isDfuFile(dfuFilePath))
            {
                return new Version(0xFF, 0xFF);
            }

            return GetVersionFromFileName(dfuFilePath);
            // TODO(lbayes): Get the DFU version from the file contents instead.
            // return Dfu.ParseFile(dfuFilePath).DeviceInfo.ProductVersion;
        }

        public bool ShouldUpdateFirmware(string dfuFilePath)
        {
            if (!isDfuFile(dfuFilePath))
            {
                return false;
            }
            return true;
        }

        public DfuResponse UpdateFirmware(string dfuFilePath)
        {
            if (!isDfuFile(dfuFilePath))
            {
                return DfuResponse.NOT_DFU_FILE;
            }

            return DfuResponse.SUCCESS;
        }
    }
}
