# macosrepro

A minimal repro for [dotnet/runtime#131944](https://github.com/dotnet/runtime/issues/131944), meant to be run on the
macOS machines offered by GitHub Actions.

## Layout

| Project | Description |
| --- | --- |
| [`src/ProcessKillRepro`](src/ProcessKillRepro) | Starts `/bin/sleep 60` with `Process.Start`, calls `Process.Kill(entireProcessTree: true)`, waits for the sleeping child to exit, reports its exit code and exits. |
| [`src/Watchdog`](src/Watchdog) | Starts the given app, waits 10 seconds for it to exit and if it does not, attaches `lldb` to it and prints the stacks of all its threads to the standard output. |

The [`macOS repro`](.github/workflows/macos-repro.yml) workflow runs only on `macos-latest`, installs the latest
.NET 11 RC SDK and runs `ProcessKillRepro` under the `Watchdog`.

## Running it locally

```sh
dotnet build src/ProcessKillRepro/ProcessKillRepro.csproj -c Release
dotnet build src/Watchdog/Watchdog.csproj -c Release
./src/Watchdog/bin/Release/net11.0/Watchdog ./src/ProcessKillRepro/bin/Release/net11.0/ProcessKillRepro
```

The `Watchdog` exits with the exit code of the app when it exits in time and with `1` when it hangs
(after dumping the stacks and killing it), so a hang fails the CI run.
