using System;
using System.IO;

namespace EightAmps
{
    public interface IDfuUpdater
    {
        event EventHandler<ErrorEventArgs> DeviceError;
        event Action<int> DfuProgressChanged;
        event Action<DfuResponse> DfuCompleted;
        Version GetConnectedAspenVersion();
        Version GetConnectedMapleVersion();
        Version GetConnectedVersion(int vid, int pid);
        DfuResponse UpdateAspenFirmware(string dfuFilePath, bool forceVersion = false, bool installDriver = true);
        DfuResponse UpdateMapleFirmware(string dfuFilePath, bool forceVersion = false, bool installDriver = true);
        Version GetFirmwareVersionFromDfu(string dfuFilePath);
        DfuResponse ShouldUpdateFirmware(string dfuFilePath, DeviceProgramming.Dfu.Device device, bool forceVersion = false, bool installDriver = true);
        DfuResponse UpdateFirmware(string dfuFilePath, int vid, int pid, bool forceVersion = false, bool installDriver = true);
        void InstallDriversFor(int vid, int pid);
    }
}