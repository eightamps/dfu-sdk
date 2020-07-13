using System;
using System.Text.RegularExpressions;
using DeviceProgramming.Dfu;

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

        public Version GetFirmwareVersion(string dfuFilePath)
        {
            return new Version(2, 3);
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
