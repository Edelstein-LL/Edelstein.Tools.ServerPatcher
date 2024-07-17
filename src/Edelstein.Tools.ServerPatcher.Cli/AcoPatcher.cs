using Edelstein.Assets.Management.Manifest;

using System.Text;
using System.Text.Json;

namespace Edelstein.Tools.ServerPatcher.Cli;

public class AcoPatcher
{
    private static readonly JsonSerializerOptions IndentedJsonSerializerOptions = new() { WriteIndented = true };

    public static async Task Patch(Stream acoAssetStream, Uri apiUri, Uri assetsUri, GameRegion gameRegion)
    {
        const int acoOffset = 4164;
        const int jpSerializedAcoLength = 578;
        const int glSerializedAcoLength = 491;

        int requiredSerializedAcoLength = gameRegion is GameRegion.Jp ? jpSerializedAcoLength : glSerializedAcoLength;

        acoAssetStream.Seek(acoOffset, SeekOrigin.Begin);

        int acoDataSize = (int)acoAssetStream.Length - acoOffset;

        byte[] acoData = new byte[acoDataSize];

        int readBytesCount = await acoAssetStream.ReadAsync(acoData);

        if (readBytesCount != acoDataSize)
            throw new InvalidDataException();

        using MemoryStream encryptedAcoStream = new(acoData);
        using MemoryStream decryptedAcoStream = new();

        await ManifestCryptor.DecryptUncompressedAsync(encryptedAcoStream, decryptedAcoStream);
        encryptedAcoStream.Seek(0, SeekOrigin.Begin);
        decryptedAcoStream.Seek(0, SeekOrigin.Begin);

        AccessConfigObject aco = (await JsonSerializer.DeserializeAsync<AccessConfigObject>(decryptedAcoStream))!;
        decryptedAcoStream.Seek(0, SeekOrigin.Begin);

        aco.InquiryServer = aco.ApiServer = apiUri + "/";
        aco.AlbumCdnServer = aco.CdnServer = assetsUri + "/";
        aco.MaintenanceFile = aco.CdnServer + "maintenance/maintenance.json";

        string serializedAco = JsonSerializer.Serialize(aco, IndentedJsonSerializerOptions);

        serializedAco = serializedAco.ReplaceLineEndings("\n");
        serializedAco = serializedAco.Replace("  ", "    ");

        if (serializedAco.Length > requiredSerializedAcoLength)
            throw new InvalidDataException();

        if (serializedAco.Length < requiredSerializedAcoLength)
            serializedAco += new string(' ', requiredSerializedAcoLength - serializedAco.Length);

        await decryptedAcoStream.WriteAsync(Encoding.UTF8.GetBytes(serializedAco));
        decryptedAcoStream.Seek(0, SeekOrigin.Begin);

        await ManifestCryptor.EncryptUncompressedAsync(decryptedAcoStream, encryptedAcoStream);
        encryptedAcoStream.Seek(0, SeekOrigin.Begin);

        acoAssetStream.Seek(acoOffset, SeekOrigin.Begin);

        await encryptedAcoStream.CopyToAsync(acoAssetStream);
    }
}
