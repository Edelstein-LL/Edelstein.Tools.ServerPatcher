using Edelstein.Tools.ServerPatcher.Wasm.Components;
using Edelstein.Tools.ServerPatcher.Wasm.Core;
using Edelstein.Tools.ServerPatcher.Wasm.Extensions;

using KristofferStrube.Blazor.Streams;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

using System.IO.Compression;
using System.Text;

namespace Edelstein.Tools.ServerPatcher.Wasm.Pages;

public partial class Patcher
{
    private ValidationMessageStore? _validationMessageStore;
    private EditContext? _editContext;

    private FileInfo? _apkFile;
    private string? _customApkOriginalFileName;

    private Stream? _apkStream;

    [Inject]
    private HttpClient HttpClient { get; set; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [SupplyParameterFromForm]
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
    private PatchSettings PatchSettings { get; set; } = new();

    private bool _useCustomApk;
    private bool _isCustomApkUploaded;

    private bool UseCustomApk
    {
        get => _useCustomApk;
        set
        {
            _useCustomApk = value;

            IsPatchDisabled = !(!value || IsCustomApkUploaded);
        }
    }

    private bool IsCustomApkUploaded
    {
        get => _isCustomApkUploaded;
        set
        {
            _isCustomApkUploaded = value;

            IsPatchDisabled = !(!UseCustomApk || value);
        }
    }

    private bool IsPatchDisabled { get; set; }

    private string? PatchedApkDownloadUri { get; set; }

    protected override void OnInitialized()
    {
        _editContext = new EditContext(PatchSettings);
        _validationMessageStore = new ValidationMessageStore(_editContext);
        _editContext.OnValidationStateChanged += OnFormValidationRequested;
    }

    private void OnFormValidationRequested(object? sender, ValidationStateChangedEventArgs e)
    {
        _validationMessageStore?.Clear();

        if (!String.IsNullOrEmpty(PatchSettings.ApiUri) && !Uri.TryCreate(PatchSettings.ApiUri, UriKind.Absolute, out _))
            _validationMessageStore?.Add(() => PatchSettings.ApiUri!, "Provided string is not a valid URL");

        if (!String.IsNullOrEmpty(PatchSettings.AssetsUri) && !Uri.TryCreate(PatchSettings.AssetsUri, UriKind.Absolute, out _))
            _validationMessageStore?.Add(() => PatchSettings.AssetsUri!, "Provided string is not a valid URL");
    }

    private void OnCustomApkUploadComplete(IEnumerable<FluentInputFileEventArgs> args)
    {
        FluentInputFileEventArgs customApkEventArgs = args.First();

        if (customApkEventArgs.IsCancelled)
            return;

        _customApkOriginalFileName = customApkEventArgs.Name;

        IsCustomApkUploaded = true;

        _apkFile = customApkEventArgs.LocalFile;
    }

    private void OnCustomApkUploadProgress(FluentInputFileEventArgs args)
    {
        if (!UseCustomApk)
            args.IsCancelled = true;
    }

    private async Task OnValidFormSubmit()
    {
        if (UseCustomApk && _apkFile is null)
            throw new InvalidOperationException("Custom .apk was requested to be used and no file was provided.");

        PatchSettings.ApiUri ??= "http://localhost:35373";
        PatchSettings.AssetsUri ??= "http://localhost:35373";

        PatchSettings.ApiUri = PatchSettings.ApiUri.TrimEnd('/');
        PatchSettings.AssetsUri = PatchSettings.AssetsUri.TrimEnd('/');

        IDialogReference dialog = await DialogService.ShowDialogAsync<PatcherDialog>(new PatcherDialog.DialogReferenceContent<string>(),
            new DialogParameters
            {
                Title = "Patching .apk",
                PreventDismissOnOverlayClick = true,
                TrapFocus = true,
                Modal = true,
                PreventScroll = true,
                ShowDismiss = false
            });

        if (!UseCustomApk)
        {
            Console.WriteLine("Downloading base .apk...");
            ((PatcherDialog.DialogReferenceContent<string>)dialog.Instance.Content).Content = "Downloading base .apk...";

            _apkFile = new FileInfo(Path.GetTempFileName());

            Uri baseApkUri = PatchSettings.BuildBaseApkUri();

            FileStream apkStream = File.Open(_apkFile!.FullName, FileMode.Open, FileAccess.ReadWrite);
            await using IJSInProcessObjectReference jsStreamReference =
                await JsRuntime.InvokeAsync<IJSInProcessObjectReference>("getRemoteFileStream", baseApkUri);
            await using (ReadableStreamInProcess baseApkRemoteStream =
                await ReadableStreamInProcess.CreateAsync(JsRuntime, jsStreamReference))
            {
                await baseApkRemoteStream.CopyToWithProgressOfUnknownLengthAsync(apkStream,
                    new Progress<long>(progress =>
                        ((PatcherDialog.DialogReferenceContent<string>)dialog.Instance.Content).Content =
                        $"Downloading base .apk: {progress / 1024.0 / 1024.0:F3} / {(PatchSettings.Region is GameRegion.Jp ? 136 : 488)} MiB"));
            }

            apkStream.Seek(0, SeekOrigin.Begin);

            _apkStream = apkStream;

            Console.WriteLine("Base .apk downloaded successfully");
            ((PatcherDialog.DialogReferenceContent<string>)dialog.Instance.Content).Content = "Base .apk downloaded successfully";
        }
        else
        {
            _apkStream = File.Open(_apkFile!.FullName, FileMode.Open, FileAccess.ReadWrite);

            Console.WriteLine("Using custom base .apk");
            ((PatcherDialog.DialogReferenceContent<string>)dialog.Instance.Content).Content = "Using custom base .apk";
        }

        using (ZipArchive zipArchive = new(_apkStream, ZipArchiveMode.Update, true))
        {
            Console.WriteLine("Patching GGL URI...");
            ((PatcherDialog.DialogReferenceContent<string>)dialog.Instance.Content).Content = "Patching GGL URI...";

            ZipArchiveEntry? gglUrlEntry = zipArchive.GetEntry("assets/ggl_url.txt");

            if (gglUrlEntry is null)
            {
                Console.WriteLine("There is no ggl_url.txt. Is loaded .apk appropriately patched?");
                return;
            }

            await using (Stream gglUrlStream = gglUrlEntry.Open())
            {
                gglUrlStream.SetLength(0);

                await gglUrlStream.WriteAsync(Encoding.UTF8.GetBytes(PatchSettings.ApiUri));
            }

            Console.WriteLine("GGL URI was patched");

            Console.WriteLine("Patching API and assets URIs...");
            ((PatcherDialog.DialogReferenceContent<string>)dialog.Instance.Content).Content = "Patching API and assets URIs...";

            ZipArchiveEntry? accessConfigObjectEntry = zipArchive.GetEntry("assets/bin/Data/ee646842ed462d54ebacb6f00b3bd3d1");
            if (accessConfigObjectEntry is null)
            {
                Console.WriteLine("There is no AccessConfigObject. Is loaded .apk valid?");
                return;
            }

            await using (Stream acoStream = accessConfigObjectEntry.Open())
            {
                await AcoPatcher.Patch(JsRuntime, acoStream, PatchSettings.ApiUri, PatchSettings.AssetsUri, PatchSettings.Region);
            }

            Console.WriteLine("API and assets URIs was patched");
        }

        ((PatcherDialog.DialogReferenceContent<string>)dialog.Instance.Content).Content = "Finilizing file and preparing to download...";

        _apkStream.Seek(0, SeekOrigin.Begin);

        using DotNetStreamReference apkStreamReference = new(_apkStream);

        PatchedApkDownloadUri = await JsRuntime.InvokeAsync<string>("downloadFileFromDotnetStream", "sif2.apk",
            "application/vnd.android.package-archive",
            apkStreamReference);

        Console.WriteLine("Finished");

        ((PatcherDialog.DialogReferenceContent<string>)dialog.Instance.Content).Content = "Waiting for download to begin...";

        _apkFile.Delete();

        await dialog.CloseAsync();
    }
}
