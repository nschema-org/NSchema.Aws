using Amazon.S3;

namespace NSchema.Aws.Tests;

/// <summary>
/// Verifies that <c>UseS3StateStore</c>'s client factory applies the caller's <see cref="AmazonS3Config"/> delegate.
/// The AWS client constructor resolves credentials from the ambient chain, so dummy ones are set to keep it from
/// throwing where no real AWS credentials exist — no request is made, only the built client's configuration is inspected.
/// </summary>
public sealed class NSchemaApplicationBuilderExtensionsTests : IDisposable
{
    public NSchemaApplicationBuilderExtensionsTests()
    {
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "test");
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "test");
        Environment.SetEnvironmentVariable("AWS_REGION", "us-east-1");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", null);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", null);
        Environment.SetEnvironmentVariable("AWS_REGION", null);
    }

    [Fact]
    public void CreateS3Client_AppliesTheConfigureClientDelegate()
    {
        var client = NSchemaApplicationBuilderExtensions.CreateS3Client(config =>
        {
            config.ServiceURL = "http://localhost:9000";
            config.ForcePathStyle = true;
        });

        // The SDK normalizes the endpoint (it appends a trailing slash), so match the prefix rather than the literal.
        var config = client.Config.ShouldBeOfType<AmazonS3Config>();
        config.ServiceURL.ShouldStartWith("http://localhost:9000");
        config.ForcePathStyle.ShouldBeTrue();
    }

    [Fact]
    public void CreateS3Client_WithoutADelegate_DefersToAmbientConfiguration()
    {
        var client = NSchemaApplicationBuilderExtensions.CreateS3Client(configureClient: null);

        var config = client.Config.ShouldBeOfType<AmazonS3Config>();
        config.ForcePathStyle.ShouldBeFalse();
    }
}
