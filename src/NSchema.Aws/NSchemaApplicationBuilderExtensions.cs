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
        /// <param name="configureClient">An optional delegate to configure the underlying <see cref="AmazonS3Config"/></param>
        /// <returns>The <see cref="NSchemaApplicationBuilder"/> instance, allowing for method chaining.</returns>
        /// <remarks>
        /// If no <see cref="IAmazonS3"/> is registered, one is created automatically.
        /// </remarks>
        public NSchemaApplicationBuilder UseS3StateStore(string bucket, string key, Action<AmazonS3Config>? configureClient = null) => builder
            .UseS3StateStore(o => { o.Bucket = bucket; o.Key = key; }, configureClient);

        /// <summary>
        /// Configures NSchema to use an S3 object as the schema state store.
        /// </summary>
        /// <param name="configure">A delegate that configures the S3 schema state store options.</param>
        /// <param name="configureClient">An optional delegate to configure the underlying <see cref="AmazonS3Config"/>.</param>
        /// <returns>The <see cref="NSchemaApplicationBuilder"/> instance, allowing for method chaining.</returns>
        public NSchemaApplicationBuilder UseS3StateStore(Action<S3SchemaStateStoreOptions> configure, Action<AmazonS3Config>? configureClient = null) => builder
            .UseS3StateStore((o, _) => configure(o), configureClient);

        /// <summary>
        /// Configures NSchema to use an S3 object as the schema state store, with access to the <see cref="IServiceProvider"/> while configuring the options.
        /// </summary>
        /// <param name="configure">A delegate that configures the S3 schema state store options.</param>
        /// <param name="configureClient">An optional delegate to configure the underlying <see cref="AmazonS3Config"/>.</param>
        /// <returns>The <see cref="NSchemaApplicationBuilder"/> instance, allowing for method chaining.</returns>
        public NSchemaApplicationBuilder UseS3StateStore(Action<S3SchemaStateStoreOptions, IServiceProvider> configure, Action<AmazonS3Config>? configureClient = null)
        {
            builder.Services.AddOptions<S3SchemaStateStoreOptions>().Configure(configure);
            builder.Services.TryAddSingleton<IAmazonS3>(_ => CreateS3Client(configureClient));
            return builder.UseStateStore<S3SchemaStateStore>();
        }
    }

    /// <summary>
    /// Builds the <see cref="IAmazonS3"/> the state store uses.
    /// </summary>
    internal static IAmazonS3 CreateS3Client(Action<AmazonS3Config>? configureClient)
    {
        var config = new AmazonS3Config();
        configureClient?.Invoke(config);
        return new AmazonS3Client(config);
    }
}
