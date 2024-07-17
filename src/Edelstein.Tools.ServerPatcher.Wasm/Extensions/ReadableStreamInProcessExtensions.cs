using KristofferStrube.Blazor.Streams;

namespace Edelstein.Tools.ServerPatcher.Wasm.Extensions;

public static class ReadableStreamInProcessExtensions
{
    public static async Task CopyToWithProgressOfUnknownLengthAsync(this ReadableStreamInProcess source, Stream destination,
        IProgress<long> progressPercentage, CancellationToken cancellationToken = default)
    {
        long totalBytesRead = 0;

        await using ReadableStreamDefaultReaderInProcess reader = source.GetDefaultReader();

        await foreach (byte[] chunk in reader.IterateByteArraysAsync(cancellationToken))
        {
            await destination.WriteAsync(chunk.AsMemory(), cancellationToken).ConfigureAwait(false);

            totalBytesRead += chunk.Length;
            progressPercentage.Report(totalBytesRead);
        }
    }
}
