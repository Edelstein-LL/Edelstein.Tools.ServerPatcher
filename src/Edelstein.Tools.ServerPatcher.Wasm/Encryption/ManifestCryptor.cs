using Microsoft.JSInterop;

using System.Text;

namespace Edelstein.Tools.ServerPatcher.Wasm.Encryption;

public static class ManifestCryptor
{
    private const string AesKey = "akmzncej3dfheuds654sg9ad1f3fnfoi";
    private static readonly byte[] AesKeyBytes = Encoding.UTF8.GetBytes(AesKey);

    private const string AesIv = "lmxcye89bsdfb0a1";
    private static readonly byte[] AesIvBytes = Encoding.UTF8.GetBytes(AesIv);

    public static async Task DecryptUncompressedAsync(IJSRuntime jsRuntime, MemoryStream encryptedStream, MemoryStream outputStream)
    {
        byte[] inputBytes = encryptedStream.ToArray();

        byte[] outputBytes = await jsRuntime.InvokeAsync<byte[]>("decryptAes", inputBytes, AesKeyBytes, AesIvBytes);

        await outputStream.WriteAsync(outputBytes);
    }

    public static async Task EncryptUncompressedAsync(IJSRuntime jsRuntime, MemoryStream dataStream, MemoryStream outputStream)
    {
        byte[] inputBytes = dataStream.ToArray();

        byte[] outputBytes = await jsRuntime.InvokeAsync<byte[]>("encryptAes", inputBytes, AesKeyBytes, AesIvBytes);

        await outputStream.WriteAsync(outputBytes);
    }
}
