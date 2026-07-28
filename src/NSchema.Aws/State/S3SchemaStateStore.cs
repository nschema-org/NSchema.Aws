using System.Net;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using NSchema.State.Locks;
using NSchema.State.Locks.Plugins;
using NSchema.State.Plugins;

namespace NSchema.Aws.State;

/// <summary>
/// An <see cref="IDatabaseStateStore"/> that persists the schema snapshot to an S3 object, and an
/// <see cref="IStateLock"/> that coordinates exclusive access to that state via a sibling lock object.
/// </summary>
internal sealed class S3SchemaStateStore(IOptions<S3SchemaStateStoreOptions> options, IAmazonS3 s3) : IDatabaseStateStore, IStateLock
{
    private string Bucket => options.Value.Bucket;

    private string Key => options.Value.Key;

    private string LockKey => options.Value.Key + ".lock";

    // S3 reports every anticipated failure — no bucket, no credentials, no network — as AmazonS3Exception, so that is
    // what is caught and reported. Anything else escaping is a defect in this store, and the engine treats it as one.
    private const string Source = "s3";

    /// <inheritdoc />
    public async Task<Result<StoreReadResult>> Read(CancellationToken cancellationToken = default)
    {
        try
        {
            return new StoreReadResult(await ReadObject(Key, cancellationToken));
        }
        catch (AmazonS3Exception exception)
        {
            return Result.Failure<StoreReadResult>(Unreachable(exception));
        }
    }

    /// <inheritdoc />
    public async Task<Result> Write(ReadOnlyMemory<byte> state, CancellationToken cancellationToken = default)
    {
        try
        {
            await s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = Bucket,
                Key = Key,
                InputStream = new MemoryStream(state.ToArray()),
                ContentType = "application/json",
            }, cancellationToken);

            return Result.Success();
        }
        catch (AmazonS3Exception exception)
        {
            return Result.From(Unreachable(exception));
        }
    }

    private static Diagnostic Unreachable(Exception exception) =>
        Diagnostic.Error(Source, $"Could not reach the state store: {ExceptionMessage.Describe(exception):text}");

    /// <inheritdoc />
    public async Task<Result<IStateLockHandle>> Acquire(StateLockInfo info, CancellationToken cancellationToken = default)
    {
        try
        {
            await s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = Bucket,
                Key = LockKey,
                InputStream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(info)),
                ContentType = "application/json",
                // Atomic create-if-absent: S3 returns 412 if the lock object already exists.
                IfNoneMatch = "*",
            }, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            var existing = await ReadLockInfo(cancellationToken);
            throw new StateLockedException(
                existing is null
                    ? "The state is already locked by another operation."
                    : $"The state is locked by '{existing.Who}' (operation '{existing.Operation}') since {existing.CreatedUtc:u}.",
                existing!);
        }
        catch (AmazonS3Exception exception)
        {
            return Result.Failure<IStateLockHandle>(Unreachable(exception));
        }

        return Result.Success<IStateLockHandle>(new Handle(this, info));
    }

    /// <inheritdoc />
    public async Task<Result<LockPeekResult>> Peek(CancellationToken cancellationToken = default)
    {
        try
        {
            return new LockPeekResult(await ReadLockInfo(cancellationToken));
        }
        catch (AmazonS3Exception exception)
        {
            return Result.Failure<LockPeekResult>(Unreachable(exception));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Result> Release(CancellationToken cancellationToken = default)
    {
        try
        {
            await ReleaseLock(cancellationToken);
            return Result.Success();
        }
        catch (AmazonS3Exception exception)
        {
            return Result.From(Unreachable(exception));
        }
    }

    private async Task<ReadOnlyMemory<byte>?> ReadObject(string key, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await s3.GetObjectAsync(new GetObjectRequest
            {
                BucketName = Bucket,
                Key = key,
            }, cancellationToken);

            using var buffer = new MemoryStream();
            await response.ResponseStream.CopyToAsync(buffer, cancellationToken);
            return buffer.ToArray();
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task<StateLockInfo?> ReadLockInfo(CancellationToken cancellationToken)
    {
        var bytes = await ReadObject(LockKey, cancellationToken);
        return bytes is null ? null : JsonSerializer.Deserialize<StateLockInfo>(bytes.Value.Span);
    }

    private Task ReleaseLock(CancellationToken cancellationToken = default) =>
        s3.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = Bucket,
            Key = LockKey,
        }, cancellationToken);

    private sealed class Handle(S3SchemaStateStore store, StateLockInfo info) : IStateLockHandle
    {
        private int _released;

        public StateLockInfo Info => info;

        public async ValueTask<Result> Release(CancellationToken cancellationToken = default)
        {
            // Release is idempotent: only the first call deletes the lock object.
            if (Interlocked.Exchange(ref _released, 1) != 0)
            {
                return Result.Success();
            }

            try
            {
                await store.ReleaseLock(cancellationToken);
                return Result.Success();
            }
            catch (AmazonS3Exception exception)
            {
                return Result.From(Unreachable(exception));
            }
        }
    }
}
