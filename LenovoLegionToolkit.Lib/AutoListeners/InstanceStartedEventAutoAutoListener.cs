using System;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.System.Management;
using System.Management;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.AutoListeners;

public class InstanceStartedEventAutoAutoListener : AbstractAutoListener<InstanceStartedEventAutoAutoListener.ChangedEventArgs>
{
    public class ChangedEventArgs(int processId, int parentProcessId, string processName) : EventArgs
    {
        public int ProcessId { get; } = processId;
        public int ParentProcessId { get; } = parentProcessId;
        public string ProcessName { get; } = processName;
    }

    private ManagementEventWatcher? _watcher;

    protected override Task StartAsync()
    {
        try
        {
            _watcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process'"));
            _watcher.EventArrived += Watcher_EventArrived;
            _watcher.Start();
        }
        catch (Exception ex)
        {
            Log.Instance.Trace($"Failed to start process watcher.", ex);
        }

        return Task.CompletedTask;
    }

    protected override Task StopAsync()
    {
        if (_watcher != null)
        {
            try
            {
                _watcher.Stop();
                _watcher.Dispose();
            }
            catch { /* Ignore */ }
            _watcher = null;
        }

        return Task.CompletedTask;
    }

    private void Watcher_EventArrived(object sender, EventArrivedEventArgs e)
    {
        try
        {
            var targetInstance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            var processId = Convert.ToInt32(targetInstance["ProcessId"]);
            var parentProcessId = Convert.ToInt32(targetInstance["ParentProcessId"]);
            var processName = (string)targetInstance["Name"];

            var nameWithoutExt = global::System.IO.Path.GetFileNameWithoutExtension(processName);
            
            RaiseChanged(new ChangedEventArgs(processId, parentProcessId, nameWithoutExt));
        }
        catch (Exception)
        {
        }
    }
}
