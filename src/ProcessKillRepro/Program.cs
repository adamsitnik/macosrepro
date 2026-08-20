using System.Diagnostics;
using Microsoft.DotNet.RemoteExecutor;

// Repro for https://github.com/dotnet/runtime/issues/131944:
// build a process tree (this app -> RemoteExecutor child -> '/bin/sleep 600' grandchild),
// kill the entire tree and wait for the child to exit.
// A single sleeping process is not enough to reproduce the hang, so a tree is killed 10 times in a row.

if (!RemoteExecutor.IsSupported)
{
    Console.Error.WriteLine("[repro] RemoteExecutor is not supported on this platform");
    return 2;
}

for (int i = 0; i < 10; i++)
{
    KillProcessTree(i);
}

return 0;

static void KillProcessTree(int index)
{
    RemoteInvokeOptions options = new RemoteInvokeOptions { CheckExitCode = false };
    // the child writes the pid of the grandchild to the standard output and
    // blocks on reading the standard input until it gets killed.
    options.StartInfo.RedirectStandardOutput = true;
    options.StartInfo.RedirectStandardInput = true;

    using RemoteInvokeHandle handle = RemoteExecutor.Invoke(static () =>
    {
        ProcessStartInfo grandChildStartInfo = new ProcessStartInfo("/bin/sleep");
        grandChildStartInfo.ArgumentList.Add("600");

        using Process grandChild = Process.Start(grandChildStartInfo)!;
        Console.WriteLine(grandChild.Id);

        // this blocks the child until the parent kills the entire process tree
        _ = Console.ReadLine();
    }, options);

    Process child = handle.Process;
    // wait for the grandchild to get started, so the whole tree exists before it gets killed
    string? grandChildId = child.StandardOutput.ReadLine();
    Console.WriteLine($"[repro {index}] child pid {child.Id} started '/bin/sleep 600' with pid {grandChildId}");

    Stopwatch stopwatch = Stopwatch.StartNew();

    child.Kill(entireProcessTree: true);
    Console.WriteLine($"[repro {index}] Kill(entireProcessTree: true) returned after {stopwatch.ElapsedMilliseconds} ms");

    child.WaitForExit();
    Console.WriteLine($"[repro {index}] child exited with code {child.ExitCode} after {stopwatch.ElapsedMilliseconds} ms");
}
