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
        Version GetConnectedMapleVersion();
        Version GetConnectedVersion(int vid, int pid);

        void UpdateAspenFirmware(string dfuFilePath, bool forceVersion = false);
        void UpdateMapleFirmware(string dfuFilePath, bool forceVersion = false);
        Version GetFirmwareVersionFromDfu(string dfuFilePath);
        DfuResponse ShouldUpdateFirmware(string dfuFilePath, int vid, int pid, bool forceVersion = false);
        void UpdateFirmware(string dfuFilePath, int vid, int pid, bool forceVersion = false);
    }
}