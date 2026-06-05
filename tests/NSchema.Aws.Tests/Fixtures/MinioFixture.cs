using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using NSchema.State;
using Testcontainers.Minio;

namespace NSchema.Aws.Tests.Fixtures;

public sealed class MinioFixture : IAsyncLifetime
{
    private readonly MinioContainer _container = new MinioBuilder("minio/minio:latest").Build();

    public IAmazonS3 S3 { get; private set; } = null!;
    public ISchemaStateSerializer Serializer { get; private set; } = null!;
    public string BucketName { get; } = $"nschema-test-{Guid.NewGuid():N}";

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        S3 = new AmazonS3Client(
            new BasicAWSCredentials(_container.GetAccessKey(), _container.GetSecretKey()),
            new AmazonS3Config
            {
                ServiceURL = _container.GetConnectionString(),
                ForcePathStyle = true,
                AuthenticationRegion = "us-east-1",
            });

        await S3.PutBucketAsync(BucketName);

        // Resolve the default serializer from the NSchema DI container.
        Serializer = NSchemaApplication.CreateBuilder().Build().Services
            .GetRequiredService<ISchemaStateSerializer>();
    }

    public async ValueTask DisposeAsync()
    {
        S3.Dispose();
        await _container.DisposeAsync();
    }
}
