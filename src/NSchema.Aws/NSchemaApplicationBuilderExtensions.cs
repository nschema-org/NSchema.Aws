using NSchema.Aws.State;

namespace NSchema.Aws;

/// <summary>
/// Provides extension methods for configuring NSchema to integrate with AWS.
/// </summary>
public static class NSchemaApplicationBuilderExtensions
{
    extension(NSchemaApplicationBuilder builder)
    {
        /// <summary>
        /// Configures NSchema to use AWS S3 as the schema state store.
        /// </summary>
        /// <returns>The <see cref="NSchemaApplicationBuilder"/> instance, allowing for method chaining.</returns>
        public NSchemaApplicationBuilder UseAwsS3SchemaStateStore() => builder
            .UseSchemaStateStore<S3BackedSchemaStateStore>();
    }
}
