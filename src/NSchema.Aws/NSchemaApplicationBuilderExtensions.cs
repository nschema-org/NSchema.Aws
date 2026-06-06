using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        /// Configures NSchema to use an S3 object as the schema state store.
        /// </summary>
        /// <param name="bucket">The name of the S3 bucket that holds the state object.</param>
        /// <param name="key">The S3 object key for the state file within the bucket.</param>
        /// <returns>The <see cref="NSchemaApplicationBuilder"/> instance, allowing for method chaining.</returns>
        /// <remarks>
        /// If no <see cref="IAmazonS3"/> is registered, a default <see cref="AmazonS3Client"/> usingambient credentials is registered automatically.
        /// <para>
        /// State writes are last-write-wins. Concurrent applies will silently overwrite each other's state.
        /// </para>
        /// </remarks>
        public NSchemaApplicationBuilder UseS3StateStore(string bucket, string key) => builder
            .UseS3StateStore(o => { o.Bucket = bucket; o.Key = key; });

        /// <summary>
        /// Configures NSchema to use an S3 object as the schema state store with an explicit S3 client.
        /// </summary>
        /// <param name="configure">A delegate that can be used to configure the S3 schema state store options.</param>
        /// <returns>The <see cref="NSchemaApplicationBuilder"/> instance, allowing for method chaining.</returns>
        public NSchemaApplicationBuilder UseS3StateStore(Action<S3SchemaStateStoreOptions> configure) => builder
            .UseS3StateStore((o, _) => configure(o));

        /// <summary>
        /// Configures NSchema to use an S3 object as the schema state store with an explicit S3 client.
        /// </summary>
        /// <param name="configure">A delegate that can be used to configure the S3 schema state store options.</param>
        /// <returns>The <see cref="NSchemaApplicationBuilder"/> instance, allowing for method chaining.</returns>
        public NSchemaApplicationBuilder UseS3StateStore(Action<S3SchemaStateStoreOptions, IServiceProvider> configure)
        {
            builder.Services.TryAddSingleton<IAmazonS3, AmazonS3Client>();
            builder.Services.AddOptions<S3SchemaStateStoreOptions>().Configure(configure);
            return builder.UseStateStore<S3SchemaStateStore>();
        }
    }
}
