using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using NSchema.Schema;
using NSchema.State;

namespace NSchema.Aws.State;

/// <summary>
/// An <see cref="ISchemaStateStore"/> that persists the schema snapshot to an S3 object.
/// </summary>
/// <remarks>
/// Last-write-wins: concurrent applies will silently overwrite each other's state.
/// </remarks>
internal sealed class S3SchemaStateStore(IOptions<S3SchemaStateStoreOptions> options, IAmazonS3 s3, ISchemaStateSerializer serializer) : ISchemaStateStore
{
    /// <inheritdoc />
    public async Task<DatabaseSchema?> Read(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await s3.GetObjectAsync(new GetObjectRequest
            {
                BucketName = options.Value.Bucket,
                Key = options.Value.Key,
            }, cancellationToken);

            using var reader = new StreamReader(response.ResponseStream);
            return serializer.Deserialize(await reader.ReadToEndAsync(cancellationToken));
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public Task Write(DatabaseSchema schema, CancellationToken cancellationToken = default) =>
        s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = options.Value.Bucket,
            Key = options.Value.Key,
            ContentBody = serializer.Serialize(schema),
            ContentType = "application/json",
        }, cancellationToken);
}
