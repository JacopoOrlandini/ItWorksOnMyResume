using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;
using IWOMR.Application.Common.Interfaces;

namespace IWOMR.Infrastructure.Storage;

public class MinioStorageService(IMinioClient minio, IConfiguration config) : IStorageService
{
    private readonly string _bucket = config["MinIO:AvatarBucket"] ?? "iwomr-avatars";
    private readonly string _endpoint = config["MinIO:Endpoint"] ?? "localhost:9000";

    public async Task<string> UploadAsync(
        Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var objectKey = $"{Guid.NewGuid():N}/{fileName}";

        await minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectKey)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(contentType), ct);

        return objectKey;
    }

    public Task<string> GetUrlAsync(string objectKey, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        // Public bucket — direct URL (no pre-signing needed)
        return Task.FromResult($"http://{_endpoint}/{_bucket}/{objectKey}");
    }

    public async Task DeleteAsync(string objectKey, CancellationToken ct = default) =>
        await minio.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectKey), ct);
}
