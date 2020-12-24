using System.Diagnostics;

namespace EightAmps.utils
{
    public static class ProcessHelper
    {
       public static Process GetProcess(string processName, string args,
           DataReceivedEventHandler handler,
           DataReceivedEventHandler errorHandler)
        {
            var process = new Process();
            process.StartInfo.FileName = processName;
            process.StartInfo.Arguments = args;
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += handler;
            process.ErrorDataReceived += errorHandler;
            //process.Exited += 

            return process;
        }
    }
}
