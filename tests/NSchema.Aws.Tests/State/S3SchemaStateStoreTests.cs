using System.Text;
using Microsoft.Extensions.Options;
using NSchema.Aws.State;
using NSchema.Aws.Tests.Fixtures;
using NSchema.State;

namespace NSchema.Aws.Tests.State;

[Collection("minio")]
public sealed class S3SchemaStateStoreTests(MinioFixture fixture)
{
    private S3SchemaStateStore CreateSut(string? key = null) => new(
        Options.Create(new S3SchemaStateStoreOptions
        {
            Bucket = fixture.BucketName,
            Key = key ?? $"state/{Guid.NewGuid():N}.json",
        }),
        fixture.S3);

    private static ReadOnlyMemory<byte> Payload(string content) => Encoding.UTF8.GetBytes(content);

    [Fact]
    public async Task Read_MissingObject_ReturnsNull()
    {
        var sut = CreateSut();

        var result = await sut.Read(TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task Write_ThenRead_RoundTripsTheState()
    {
        var sut = CreateSut();
        var original = Payload("""{"schema":"v1"}""");

        await sut.Write(original, TestContext.Current.CancellationToken);
        var result = await sut.Read(TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Value.ToArray().ShouldBe(original.ToArray());
    }

    [Fact]
    public async Task Write_OverwritesExistingObject()
    {
        var key = $"state/{Guid.NewGuid():N}.json";
        var sut = CreateSut(key);
        var second = Payload("""{"schema":"v2"}""");

        await sut.Write(Payload("""{"schema":"v1"}"""), TestContext.Current.CancellationToken);
        await sut.Write(second, TestContext.Current.CancellationToken);
        var result = await sut.Read(TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Value.ToArray().ShouldBe(second.ToArray());
    }

    [Fact]
    public async Task Acquire_WhenUnlocked_ReturnsHandle()
    {
        var sut = CreateSut();

        await using var handle = await sut.Acquire(new StateLockRequest("apply"), TestContext.Current.CancellationToken);

        handle.LockId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Acquire_WhenAlreadyLocked_ThrowsWithExistingLockInfo()
    {
        var sut = CreateSut();
        await using var first = await sut.Acquire(new StateLockRequest("apply"), TestContext.Current.CancellationToken);

        var ex = await Should.ThrowAsync<StateLockedException>(
            () => sut.Acquire(new StateLockRequest("destroy"), TestContext.Current.CancellationToken));

        ex.ExistingLock.ShouldNotBeNull();
        ex.ExistingLock.Operation.ShouldBe("apply");
        ex.ExistingLock.Id.ShouldBe(first.LockId);
    }

    [Fact]
    public async Task Acquire_AfterReleasingHandle_Succeeds()
    {
        var key = $"state/{Guid.NewGuid():N}.json";
        var sut = CreateSut(key);

        var first = await sut.Acquire(new StateLockRequest("apply"), TestContext.Current.CancellationToken);
        await first.DisposeAsync();

        await using var second = await sut.Acquire(new StateLockRequest("apply"), TestContext.Current.CancellationToken);
        second.LockId.ShouldNotBe(first.LockId);
    }

    [Fact]
    public async Task DisposeHandle_IsIdempotent()
    {
        var sut = CreateSut();
        var handle = await sut.Acquire(new StateLockRequest("apply"), TestContext.Current.CancellationToken);

        await handle.DisposeAsync();
        await Should.NotThrowAsync(async () => await handle.DisposeAsync());
    }

    [Fact]
    public async Task ForceUnlock_WhenLocked_RemovesLockAndReturnsInfo()
    {
        var sut = CreateSut();
        await sut.Acquire(new StateLockRequest("apply"), TestContext.Current.CancellationToken);

        var removed = await sut.ForceUnlock(TestContext.Current.CancellationToken);

        removed.ShouldNotBeNull();
        removed.Operation.ShouldBe("apply");

        // The lock is now free to re-acquire.
        await using var handle = await sut.Acquire(new StateLockRequest("apply"), TestContext.Current.CancellationToken);
        handle.LockId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ForceUnlock_WhenNotLocked_ReturnsNull()
    {
        var sut = CreateSut();

        var removed = await sut.ForceUnlock(TestContext.Current.CancellationToken);

        removed.ShouldBeNull();
    }
}
