namespace NSchema.Aws.State;

/// <summary>
/// Configuration for <see cref="S3SchemaStateStore"/>.
/// </summary>
public class S3SchemaStateStoreOptions
{
    /// <summary>
    /// The name of the S3 bucket that holds the state object.
    /// </summary>
    public required string Bucket { get; set; }

    /// <summary>
    /// The S3 object key for the state file within the bucket.
    /// </summary>
    public required string Key { get; set; }
}
