using System.Diagnostics;
using Microsoft.DotNet.RemoteExecutor;

const int TreeCount = 8;
const int Iterations = 30;
TimeSpan killTimeout = TimeSpan.FromSeconds(60);
const int WaitInMilliseconds = 60_000;

if (!RemoteExecutor.IsSupported)
{
    Console.Error.WriteLine("[repro] RemoteExecutor is not supported on this platform");
    return 2;
}

for (int iteration = 0; iteration < Iterations; iteration++)
{
    RemoteInvokeHandle[] handles = new RemoteInvokeHandle[TreeCount];
    Process[] roots = new Process[TreeCount];

    try
    {
        for (int i = 0; i < TreeCount; i++)
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions { CheckExitCode = false };
            RemoteInvokeHandle handle = RemoteExecutor.Invoke(static () =>
            {
                using Process grandChild = Process.Start("/bin/sleep", "1000")!;
                Thread.Sleep(Timeout.Infinite);
            }, options);

            handles[i] = handle;
            roots[i] = handle.Process;
        }

        Thread.Sleep(500);

        Task[] tasks = new Task[TreeCount];
        for (int i = 0; i < TreeCount; i++)
        {
            Process root = roots[i];
            tasks[i] = Task.Run(() => root.Kill(entireProcessTree: true));
        }

        bool completed = Task.WaitAll(tasks, killTimeout);
        if (!completed)
        {
            throw new Exception($"Kill(entireProcessTree: true) hung on iteration {iteration}.");
        }

        foreach (Process root in roots)
        {
            bool exited = root.WaitForExit(WaitInMilliseconds);
            if (!exited)
            {
                throw new Exception($"Root process {root.Id} did not exit on iteration {iteration}.");
            }
        }
    }
    finally
    {
        foreach (RemoteInvokeHandle handle in handles)
        {
            handle?.Dispose();
        }
    }
}

return 0;
