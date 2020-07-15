using System;
using System.ComponentModel;
using System.IO;

namespace EightAmps
{
    public interface IAspen
    {
        bool IsUpdating { get; }
        event EventHandler<ErrorEventArgs> DeviceError;
        event EventHandler<ProgressChangedEventArgs> DownloadProgressChanged;

        Version GetConnectedAspenVersion();
        Version GetFirmwareVersionFromDfu(string dfuFilePath);
        DfuResponse ShouldUpdateFirmware(string dfuFilePath, bool forceVersion = false);
        DfuResponse UpdateFirmware(string dfuFilePath, bool forceVersion = false);
    }
}