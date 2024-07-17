using System.Text.Json.Serialization;

namespace Edelstein.Tools.ServerPatcher.Cli;

public class AccessConfigObject
{
    [JsonPropertyName("identifier")]
    public required string Identifier { get; set; }

    [JsonPropertyName("isDebug")]
    public bool IsDebug { get; set; }

    [JsonPropertyName("api_server")]
    public required string ApiServer { get; set; }

    [JsonPropertyName("cdn_server")]
    public required string CdnServer { get; set; }

    [JsonPropertyName("maintenance_file")]
    public required string MaintenanceFile { get; set; }

    [JsonPropertyName("inquiry_server")]
    public required string InquiryServer { get; set; }

    [JsonPropertyName("album_cdn_server")]
    public required string AlbumCdnServer { get; set; }

    [JsonPropertyName("album_cdn_server_encrypt_key")]
    public required string AlbumCdnServerEncryptKey { get; set; }
}
