namespace NSchema.Aws;

/// <summary>
/// The settings a STATE statement binds onto.
/// </summary>
internal sealed class S3Settings
{
    public string? Bucket { get; set; }
    public string? Key { get; set; }
    public bool ForcePathStyle { get; set; }
}
