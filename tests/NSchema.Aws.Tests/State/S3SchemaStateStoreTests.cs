using Microsoft.Extensions.Options;
using NSchema.Aws.State;
using NSchema.Aws.Tests.Fixtures;
using NSchema.Schema;
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
        fixture.S3,
        fixture.Serializer);

    private static DatabaseSchema SampleSchema() => DatabaseSchema.Create(
        [SchemaDefinition.Create("app", tables: [Table.Create("users", columns: [Column.Create("id", SqlType.Int)])])]);

    [Fact]
    public async Task Read_MissingObject_ReturnsNull()
    {
        var sut = CreateSut();

        var result = await sut.Read();

        result.ShouldBeNull();
    }

    [Fact]
    public async Task Write_ThenRead_RoundTripsTheSchema()
    {
        var sut = CreateSut();
        var original = SampleSchema();

        await sut.Write(original);
        var result = await sut.Read();

        result.ShouldNotBeNull();
        fixture.Serializer.Serialize(result).ShouldBe(fixture.Serializer.Serialize(original));
    }

    [Fact]
    public async Task Write_OverwritesExistingObject()
    {
        var key = $"state/{Guid.NewGuid():N}.json";
        var sut = CreateSut(key);
        var first = SampleSchema();
        var second = DatabaseSchema.Create(
            [SchemaDefinition.Create("app", tables: [Table.Create("orders", columns: [Column.Create("id", SqlType.Int)])])]);

        await sut.Write(first);
        await sut.Write(second);
        var result = await sut.Read();

        result.ShouldNotBeNull();
        result.Schemas[0].Tables[0].Name.ShouldBe("orders");
    }
}
