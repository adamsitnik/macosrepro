using System.Diagnostics;
using System.Globalization;

// Starts the given app, waits 10 seconds for it to exit and if it does not,
// attaches lldb to it and prints the stacks of all its threads to the standard output.

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: Watchdog <path-to-executable> [arguments]");
    return 2;
}

TimeSpan timeout = TimeSpan.FromSeconds(10);

ProcessStartInfo startInfo = new ProcessStartInfo(args[0]);
for (int i = 1; i < args.Length; i++)
{
    startInfo.ArgumentList.Add(args[i]);
}

using Process child = Process.Start(startInfo)!;
Console.WriteLine($"[watchdog] started '{args[0]}' with pid {child.Id}, waiting up to {timeout.TotalSeconds} seconds for it to exit");

if (child.WaitForExit(timeout))
{
    Console.WriteLine($"[watchdog] the app exited with code {child.ExitCode}");
    return child.ExitCode;
}

Console.WriteLine($"[watchdog] the app did not exit within {timeout.TotalSeconds} seconds, attaching the debugger");
PrintStacks(child.Id);

// entireProcessTree: false on purpose: it's the API under investigation and using it here
// could hang the watchdog itself. The orphaned '/bin/sleep 60' grandchild exits on its own.
Console.WriteLine("[watchdog] killing the hung app");
child.Kill(entireProcessTree: false);

return 1;

static void PrintStacks(int pid)
{
    ProcessStartInfo startInfo = new ProcessStartInfo("lldb");
    startInfo.ArgumentList.Add("--batch");
    startInfo.ArgumentList.Add("-p");
    startInfo.ArgumentList.Add(pid.ToString(CultureInfo.InvariantCulture));
    startInfo.ArgumentList.Add("-o");
    startInfo.ArgumentList.Add("thread backtrace all");
    startInfo.ArgumentList.Add("-o");
    startInfo.ArgumentList.Add("detach");
    startInfo.ArgumentList.Add("-o");
    startInfo.ArgumentList.Add("quit");

    try
    {
        // lldb inherits the standard output, so the stacks are printed directly to it.
        using Process debugger = Process.Start(startInfo)!;

        if (!debugger.WaitForExit(TimeSpan.FromMinutes(2)))
        {
            Console.Error.WriteLine("[watchdog] lldb did not finish in time, killing it");
            debugger.Kill(entireProcessTree: false);
            debugger.WaitForExit();
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[watchdog] failed to attach lldb to the process {pid}: {ex}");
    }
}
