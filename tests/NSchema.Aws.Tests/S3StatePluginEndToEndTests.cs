using System.Text;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using NSchema.Aws.Tests.Fixtures;
using NSchema.Plugins;
using NSchema.Plugins.Model.Config;
using NSchema.State.Backends;

namespace NSchema.Aws.Tests;

/// <summary>
/// End-to-end proof that the <see cref="S3StatePlugin"/> manifest wires a working state store: it builds a host
/// THROUGH the plugin's <c>Configure</c> and round-trips a payload against a real MinIO (S3-compatible) container.
/// The MinIO-pointed <see cref="IAmazonS3"/> is pre-registered so the plugin's <c>TryAdd</c> client factory defers to
/// it (rather than building one from ambient AWS config). Requires Docker.
/// </summary>
[Collection("minio")]
public sealed class S3StatePluginEndToEndTests(MinioFixture fixture)
{
    [Fact]
    public async Task Configure_ThroughThePlugin_RoundTripsStateAgainstS3()
    {
        // Arrange — a host whose S3 client points at MinIO, configured ONLY through the plugin manifest.
        var builder = NSchemaApplication.CreateBuilder();
        builder.Services.AddSingleton(fixture.S3);

        var key = $"e2e/{Guid.NewGuid():N}.json";
        var configured = new S3StatePlugin().Configure(builder, new PluginConfig("s3", new Dictionary<AttributeKey, ConfigValue>
        {
            [new AttributeKey("bucket")] = ConfigValue.OfString(fixture.BucketName),
            [new AttributeKey("key")] = ConfigValue.OfString(key),
        }));
        configured.IsSuccess.ShouldBeTrue();

        using var app = builder.Build();
        var store = app.Services.GetRequiredService<IDatabaseStateStore>();
        var payload = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes("""{"schema":"e2e"}"""));

        // Act — round-trip through the plugin-wired store.
        await store.Write(payload, TestContext.Current.CancellationToken);
        var read = await store.Read(TestContext.Current.CancellationToken);

        // Assert
        read.ShouldNotBeNull();
        read.Value.ToArray().ShouldBe(payload.ToArray());
    }
}
