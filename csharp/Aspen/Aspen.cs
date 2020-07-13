using System;
using DeviceProgramming.Dfu;

namespace EightAmps
{
    public enum DfuResponse
    {
        SUCCESS = 0,
        VERSION_IGNORED,
        TIMEOUT = 10,
        CONNECTION_FAILURE = 50,
        UNKNOWN_FAILURE,
    }

    public class Aspen
    {
        public Aspen()
        {
            Console.WriteLine("YOOO");
        }

        public Aspen(Device device)
        {
        }

        public bool ShouldUpdateFirmware(string pathToFile)
        {
            return true;
        }

        public DfuResponse UpdateFirmware(string pathToFile)
        {
            return DfuResponse.SUCCESS;
        }
    }
}
