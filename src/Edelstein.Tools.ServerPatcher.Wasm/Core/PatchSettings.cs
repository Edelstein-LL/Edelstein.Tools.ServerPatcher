namespace Edelstein.Tools.ServerPatcher.Wasm.Core;

public class PatchSettings
{
    public string? ApiUri { get; set; }

    public string? AssetsUri { get; set; }

    public bool AllowHttp
    {
        get
        {
            if (ApiUri is null)
                return true;

            if (AssetsUri is null)
                return true;

            return new Uri(ApiUri).Scheme == "http" || new Uri(AssetsUri).Scheme == "http";
        }
    }

    public GGLHeaderFormat HeaderFormat { get; set; }

    public GameRegion Region { get; set; }

    public Uri BuildBaseApkUri()
    {
        string baseApkName = "sif2_";

        baseApkName += Region is GameRegion.Jp ? "jp_base" : "gl_base";

        if (AllowHttp)
            baseApkName += "_cleartext";

        baseApkName += HeaderFormat switch
        {
            GGLHeaderFormat.Original => "",
            GGLHeaderFormat.Canonical => "_hcan",
            GGLHeaderFormat.Lowercase => "_hlow",
            _ => throw new ArgumentOutOfRangeException()
        };

        baseApkName += ".apk";

        return new Uri(new Uri("https://arasfon.ru/direct/lovelive/sif2/server-patcher/base-apks/"), baseApkName);
    }
}
