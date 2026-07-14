using DailyOneRosterFile.Api.Interfaces;
using DailyOneRosterFile.Api.Models;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace DailyOneRosterFile.Api.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly IMinioClient? _minioClient;
    private readonly string _bucketName;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IOptions<StorageOptions> storageOptions, ILogger<BlobStorageService> logger)
    {
        _logger = logger;
        var options = storageOptions.Value;
        _bucketName = options.MinioBucketName;

        if (!options.UseMinio)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.MinioEndpoint) ||
            string.IsNullOrWhiteSpace(options.MinioAccessKey) ||
            string.IsNullOrWhiteSpace(options.MinioSecretKey) ||
            string.IsNullOrWhiteSpace(options.MinioBucketName))
        {
            throw new InvalidOperationException("MinIO is enabled, but endpoint, credentials, or bucket name are missing.");
        }

        var endpoint = options.MinioEndpoint.Trim();
        var useSsl = true;
        if (endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            useSsl = false;
            endpoint = endpoint["http://".Length..];
        }
        else if (endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = endpoint["https://".Length..];
        }

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(options.MinioAccessKey, options.MinioSecretKey)
            .WithSSL(useSsl)
            .Build();
    }

    public async Task<string> UploadFileAsync(string fileName, byte[] content)
    {
        var client = GetClient();
        await EnsureBucketExistsAsync(client);

        using var ms = new MemoryStream(content);
        var putObjectArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithStreamData(ms)
            .WithObjectSize(ms.Length)
            .WithContentType("application/zip");

        await client.PutObjectAsync(putObjectArgs);

        return fileName;
    }

    public async Task<byte[]> DownloadFileAsync(string fileName)
    {
        var client = GetClient();
        using var ms = new MemoryStream();

        var getObjectArgs = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithCallbackStream(async (stream, cancellationToken) =>
            {
                await stream.CopyToAsync(ms, cancellationToken);
            });

        await client.GetObjectAsync(getObjectArgs);

        return ms.ToArray();
    }

    public async Task<string> GetLatestFileNameAsync()
    {
        var client = GetClient();
        await EnsureBucketExistsAsync(client);

        var listObjectArgs = new ListObjectsArgs()
            .WithBucket(_bucketName)
            .WithRecursive(true);

        string latestFileName = string.Empty;
        DateTime latestModified = DateTime.MinValue;

        await foreach (var item in client.ListObjectsEnumAsync(listObjectArgs))
        {
            if (item.IsDir)
            {
                continue;
            }

            var itemModified = item.LastModifiedDateTime ?? DateTime.MinValue;
            if (itemModified > latestModified)
            {
                latestModified = itemModified;
                latestFileName = item.Key;
            }
        }

        return latestFileName;
    }

    public async Task<bool> FileExistsAsync(string key)
    {
        var client = GetClient();
        try
        {
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(key);

            await client.StatObjectAsync(statObjectArgs);
            return true;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogDebug(ex, "Object '{Key}' not found in bucket '{Bucket}'.", key, _bucketName);
            return false;
        }
    }

    private IMinioClient GetClient()
    {
        return _minioClient ?? throw new InvalidOperationException("MinIO storage is not configured.");
    }

    private async Task EnsureBucketExistsAsync(IMinioClient client)
    {
        var bucketExistsArgs = new BucketExistsArgs().WithBucket(_bucketName);
        var bucketExists = await client.BucketExistsAsync(bucketExistsArgs);
        if (bucketExists)
        {
            return;
        }

        var makeBucketArgs = new MakeBucketArgs().WithBucket(_bucketName);
        await client.MakeBucketAsync(makeBucketArgs);
    }
}
