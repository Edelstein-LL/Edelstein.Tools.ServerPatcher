using Edelstein.Tools.ServerPatcher.Cli;
using Edelstein.Tools.ServerPatcher.Extensions;

using Spectre.Console;

using System.CommandLine;
using System.IO.Compression;
using System.Text;

Option<GameRegion> gameRegionOption = new(["--region", "-r"], () => GameRegion.Jp, "Game region");
Option<Uri> apiUriOption = new(["--api-url", "-a"], () => new Uri("http://localhost:35373"), "API URL");
Option<Uri> assetsUriOption = new(["--assets-url", "-c"], () => new Uri("http://localhost:35373"), "Assets URL");
Option<GGLHeaderFormat> headerFormatOption = new(["--header-format", "-h"], () => GGLHeaderFormat.Original,
    "Header format (ew uses Lowercase, Edelstein uses Original)");
Option<FileInfo?> customApkFileOption = new(["--input-file", "-i"], () => null, "Custom .apk file path");
Option<FileInfo> outputFileOption = new(["--output-file", "-o"], () => new FileInfo("sif2_patched.apk"), "Output .apk file path");

RootCommand rootCommand = [gameRegionOption, apiUriOption, assetsUriOption, headerFormatOption, customApkFileOption, outputFileOption];

rootCommand.SetHandler(HandleRootCommand, gameRegionOption, apiUriOption, assetsUriOption, headerFormatOption,
    customApkFileOption, outputFileOption);

return await rootCommand.InvokeAsync(args);

static async Task<int> HandleRootCommand(GameRegion region, Uri apiUri, Uri assetsUri, GGLHeaderFormat headerFormat, FileInfo? customApk,
    FileInfo outputFile)
{
    if (customApk is null)
    {
        AnsiConsole.WriteLine("Downloading base .apk...");

        bool allowHttp = apiUri.Scheme == "http" || assetsUri.Scheme == "http";

        await using FileStream apkFileStream = outputFile.Create();
        using HttpClient httpClient = new();
        HttpResponseMessage baseApkResponse = await httpClient.GetAsync(BuildBaseApkUri(region, allowHttp, headerFormat),
            HttpCompletionOption.ResponseHeadersRead);
        await using Stream baseApkRemoteStream = await baseApkResponse.Content.ReadAsStreamAsync();

        await AnsiConsole.Progress()
            .AutoClear(true)
            .HideCompleted(true)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new RemainingTimeColumn(),
                new SpinnerColumn())
            .StartAsync(async context =>
            {
                ProgressTask downloadTask = context.AddTask("Base .apk");

                await baseApkRemoteStream.CopyToWithProgressAsync(apkFileStream,
                    baseApkResponse.Content.Headers.ContentLength ?? (region is GameRegion.Jp ? 136 * 1024 * 1024 : 488 * 1024 * 1024),
                    new Progress<double>(progress =>
                    {
                        downloadTask.Increment(progress - downloadTask.Value);
                    }));
            });

        AnsiConsole.WriteLine("Base .apk downloaded successfully");
    }
    else
    {
        AnsiConsole.WriteLine("Using custom .apk");
        customApk.CopyTo(outputFile.FullName, true);
    }

    await using FileStream apkStream = outputFile.Open(FileMode.Open, FileAccess.ReadWrite);
    using (ZipArchive zipArchive = new(apkStream, ZipArchiveMode.Update, true))
    {
        Console.WriteLine("Patching GGL URI...");
        ZipArchiveEntry? gglUrlEntry = zipArchive.GetEntry("assets/ggl_url.txt");

        if (gglUrlEntry is null)
        {
            AnsiConsole.MarkupLine("[red]There is no ggl_url.txt. Is loaded .apk appropriately patched?[/]");
            return 1;
        }

        await using (Stream gglUrlStream = gglUrlEntry.Open())
        {
            gglUrlStream.SetLength(0);

            await gglUrlStream.WriteAsync(Encoding.UTF8.GetBytes(apiUri.ToString()));
        }

        Console.WriteLine("GGL URI was patched");
        Console.WriteLine("Patching API and assets URIs...");

        ZipArchiveEntry? accessConfigObjectEntry = zipArchive.GetEntry("assets/bin/Data/ee646842ed462d54ebacb6f00b3bd3d1");
        if (accessConfigObjectEntry is null)
        {
            AnsiConsole.MarkupLine("[red]There is no AccessConfigObject. Is loaded .apk valid?[/]");
            return 1;
        }

        await using (Stream acoStream = accessConfigObjectEntry.Open())
        {
            await AcoPatcher.Patch(acoStream, apiUri, assetsUri, region);
        }

        Console.WriteLine("API and assets URIs was patched successfully!");
    }

    return 0;
}

static Uri BuildBaseApkUri(GameRegion region, bool allowHttp, GGLHeaderFormat headerFormat)
{
    string baseApkName = "sif2_";

    baseApkName += region is GameRegion.Jp ? "jp_base" : "gl_base";

    if (allowHttp)
        baseApkName += "_cleartext";

    baseApkName += headerFormat switch
    {
        GGLHeaderFormat.Original => "",
        GGLHeaderFormat.Canonical => "_hcan",
        GGLHeaderFormat.Lowercase => "_hlow",
        _ => throw new ArgumentOutOfRangeException(nameof(headerFormat))
    };

    baseApkName += ".apk";

    return new Uri(new Uri("https://arasfon.ru/direct/lovelive/sif2/server-patcher/base-apks/"), baseApkName);
}
