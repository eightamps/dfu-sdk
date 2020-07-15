using System;
using System.ComponentModel;
using System.IO;

namespace EightAmps
{
    public interface IAspen
    {
        bool IsUpdating { get; }
        event EventHandler<ErrorEventArgs> DeviceError;
        event Action<int> DownloadProgressChanged;
        event Action<DfuResponse> DownloadCompleted;
        Version GetConnectedAspenVersion();
        Version GetFirmwareVersionFromDfu(string dfuFilePath);
        DfuResponse ShouldUpdateFirmware(string dfuFilePath, bool forceVersion = false);
        void UpdateFirmware(string dfuFilePath, bool forceVersion = false);
    }
}