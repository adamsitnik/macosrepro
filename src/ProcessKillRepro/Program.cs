using System.Diagnostics;

// Repro for https://github.com/dotnet/runtime/issues/131944:
// start a sleeping child process, kill the entire process tree and wait for the child to exit.

ProcessStartInfo startInfo = new ProcessStartInfo("/bin/sleep");
startInfo.ArgumentList.Add("60");

using Process sleeper = Process.Start(startInfo)!;
Console.WriteLine($"[child] started '/bin/sleep 60' with pid {sleeper.Id}");

Stopwatch stopwatch = Stopwatch.StartNew();

sleeper.Kill(entireProcessTree: true);
Console.WriteLine($"[child] Kill(entireProcessTree: true) returned after {stopwatch.ElapsedMilliseconds} ms");

sleeper.WaitForExit();
Console.WriteLine($"[child] sleep exited with code {sleeper.ExitCode} after {stopwatch.ElapsedMilliseconds} ms");

return 0;
