# macosrepro

A minimal repro for [dotnet/runtime#131944](https://github.com/dotnet/runtime/issues/131944), meant to be run on the
macOS machines offered by GitHub Actions.

## Layout

| Project | Description |
| --- | --- |
| [`src/ProcessKillRepro`](src/ProcessKillRepro) | Runs the following 10 times in a row: uses [`Microsoft.DotNet.RemoteExecutor`](https://github.com/dotnet/dotnet/tree/main/src/arcade/src/Microsoft.DotNet.RemoteExecutor) to start a child process which starts a `/bin/sleep 600` grandchild, calls `Process.Kill(entireProcessTree: true)` on the child, waits for it to exit and reports its exit code. |
| [`src/Watchdog`](src/Watchdog) | Starts the given app, waits 10 seconds for it to exit and if it does not, attaches `lldb` to it and prints the stacks of all its threads to the standard output. |

`Microsoft.DotNet.RemoteExecutor` is published only to the `dotnet-eng` feed, which is configured in [`NuGet.config`](NuGet.config).

The [`macOS repro`](.github/workflows/macos-repro.yml) workflow runs only on `macos-latest`, installs the latest
.NET 11 SDK and runs `ProcessKillRepro` under the `Watchdog`.

## Running it locally

```sh
dotnet build src/ProcessKillRepro/ProcessKillRepro.csproj -c Release
dotnet build src/Watchdog/Watchdog.csproj -c Release
./src/Watchdog/bin/Release/net11.0/Watchdog ./src/ProcessKillRepro/bin/Release/net11.0/ProcessKillRepro
```

The `Watchdog` exits with the exit code of the app when it exits in time and with `1` when it hangs
(after dumping the stacks and killing it), so a hang fails the CI run.
