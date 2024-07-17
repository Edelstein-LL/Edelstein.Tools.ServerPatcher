# Edelstein.Tools.ServerPatcher

Edelstein.Tools.ServerPatcher is a web and command-line tool to patch .apk of Love Live SIF2.

It can patch server URIs for both JP and GL versions of the game.

> [!NOTE]
> Signing is not done automatically, you need to do it yourself using [uber-apk-signer](https://github.com/patrickfav/uber-apk-signer) or [apksigner](https://developer.android.com/tools/apksigner).

Trailing slash is handled automatically.

## Web version

Web version is available on <https://arasfon.ru/sif2/patcher>.

All manipulations happen entirely in-browser; no data is sent to the server.

## CLI Install

CLI version of the program requires the [.NET 8.0 runtime](https://dot.net/download) to run.

Download respective [latest release](https://github.com/Edelstein-LL/Edelstein.Tools.ServerPatcher/releases/latest) executable for your OS and architecture.

## CLI Usage

```bash
./Edelstein.Tools.ServerPatcher [options]
```

Options:

- `-r, --region <Global|Jp>`                            Game region [default: `Jp`]
- `-a, --api-url <api-url>`                             API URL [default: `http://localhost:35373/`]
- `-c, --assets-url <assets-url>`                       Assets URL [default: `http://localhost:35373/`]
- `-h, --header-format <Canonical|Lowercase|Original>`  Header format (ew uses Lowercase, Edelstein uses Original) [default: `Original`]
- `-i, --input-file <input-file>`                       Custom .apk file path []
- `-o, --output-file <output-file>`                     Output .apk file path [default: `sif2_patched.apk`]
- `--version`                                           Show version information
- `-?`, `-h`, `--help`                                      Show help and usage information

## License

See [LICENSE](LICENSE)

## Used libraries

- Edelstein
  - [Edelstein.Assets.Management](https://github.com/Edelstein-LL/Edelstein.Assets.Management)
- [Spectre.Console](https://github.com/spectreconsole/spectre.console)
- [System.CommandLine](https://github.com/dotnet/command-line-api)
- [KristofferStrube.Blazor.Streams](https://github.com/KristofferStrube/Blazor.Streams)
