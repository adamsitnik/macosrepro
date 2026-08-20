using System.Diagnostics;

// Repro for https://github.com/dotnet/runtime/issues/131944:
// start a sleeping child process, kill the entire process tree and wait for the child to exit.
// A single process is not enough to reproduce the hang, so it's done 10 times in parallel.

Parallel.For(0, 10, KillSleepingProcess);

return 0;

static void KillSleepingProcess(int index)
{
    ProcessStartInfo startInfo = new ProcessStartInfo("/bin/sleep");
    startInfo.ArgumentList.Add("60");

    using Process sleeper = Process.Start(startInfo)!;
    Console.WriteLine($"[child {index}] started '/bin/sleep 60' with pid {sleeper.Id}");

    Stopwatch stopwatch = Stopwatch.StartNew();

    sleeper.Kill(entireProcessTree: true);
    Console.WriteLine($"[child {index}] Kill(entireProcessTree: true) returned after {stopwatch.ElapsedMilliseconds} ms");

    sleeper.WaitForExit();
    Console.WriteLine($"[child {index}] sleep exited with code {sleeper.ExitCode} after {stopwatch.ElapsedMilliseconds} ms");
}
