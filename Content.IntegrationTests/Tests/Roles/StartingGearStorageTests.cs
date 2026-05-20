#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared.Roles;
using Content.Server.Storage.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Collections;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;

namespace Content.IntegrationTests.Tests.Roles;

[TestFixture]
public sealed class StartingGearPrototypeStorageTest : GameTest
{
    [SidedDependency(Side.Server)] private StorageSystem _sStorageSystem = null!;

    private static readonly string[] StartingGearPrototypes = GameDataScrounger.PrototypesOfKind<StartingGearPrototype>();

    /// <summary>
    /// Checks that a storage fill on a <see cref="StartingGearPrototype"/> will properly fill.
    /// </summary>
    [TestCaseSource(nameof(StartingGearPrototypes))]
    [Description($"Checks that a storage fill on a {nameof(StartingGearPrototype)} will properly fill.")]
    [RunOnSide(Side.Server)]
    public async Task TestStartingGearStorage(string startingGearProtoId)
    {
        var gearProto = SProtoMan.Index<StartingGearPrototype>(startingGearProtoId);

        var ents = new ValueList<EntityUid>();

        foreach (var (slot, entProtos) in gearProto.Storage)
        {
            ents.Clear();
            var storageProto = ((IEquipmentLoadout)gearProto).GetGear(slot);
            if (string.IsNullOrEmpty(storageProto))
                continue;

            if (entProtos.Count == 0)
                continue;

            var bag = SSpawn(storageProto);

            foreach (var ent in entProtos)
            {
                ents.Add(SSpawn(ent));
            }

            foreach (var ent in ents)
            {
                if (!_sStorageSystem.CanInsert(bag, ent, out var reason))
                    Assert.Fail($"{nameof(StartingGearPrototype)} {gearProto.ID} could not successfully put item {SEntMan.ToPrettyString(ent)} into storage {bag.Id}. Reason: {reason ?? ""}");

                SEntMan.DeleteEntity(ent);
            }
            SEntMan.DeleteEntity(bag);
        }
    }
}
