using Content.Server.GameTicking;
using Content.Server.Maps;
using Robust.Shared.EntitySerialization;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.MetaData;

[Explicit]
public sealed class LocalizedMetaDataTests
{
    private static readonly string[] GameMaps =
    [
        "Amber",
        "Bagel",
        "Box",
        "Elkridge",
        "Fland",
        "Marathon",
        "Oasis",
        "Packed",
        "Plasma",
        "Reach",
        "Relic",
        //"Saltern",
    ];

    [Test, TestCaseSource(nameof(GameMaps))]
    public async Task TestStationStartingPowerWindow(string mapProtoId)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Dirty = true,
        });
        var server = pair.Server;

        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;
        var ticker = entMan.System<GameTicker>();

        // Load the map
        await server.WaitAssertion(() =>
        {
            Assert.That(protoMan.TryIndex<GameMapPrototype>(mapProtoId, out var mapProto));
            var opts = DeserializationOptions.Default with { InitializeMaps = true };
            ticker.LoadGameMap(mapProto, out var mapId, opts);
        });

        // Gets all entities...
        var metaQuery = entMan.EntityQueryEnumerator<MetaDataComponent>();
        while (metaQuery.MoveNext(out var uid, out var meta))
        {
            var protoId = meta.EntityPrototype;

            if (protoId.Name != meta.EntityName)
                Assert.That(protoId.Name, Is.Not.EqualTo(meta.EntityName), $"Name of the {uid.Id} and its prototype are different!");

            if (protoId.Description != meta.EntityDescription)
                Assert.That(protoId.Description, Is.Not.EqualTo(meta.EntityDescription), $"Description of the {uid.Id} and its prototype are different!");
        }

        await pair.CleanReturnAsync();
    }
}
