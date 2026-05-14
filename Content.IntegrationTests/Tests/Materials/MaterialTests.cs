#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Server.Stack;
using Content.Shared.Stacks;
using Content.Shared.Materials;
using Robust.Shared.GameObjects;
using Content.IntegrationTests.Fixtures.Attributes;

namespace Content.IntegrationTests.Tests.Materials;

/// <summary>
/// Materials and stacks have some odd relationships to entities,
/// so we need some test coverage for them.
/// </summary>
[TestOf(typeof(StackSystem))]
[TestOf(typeof(MaterialPrototype))]
public sealed class MaterialPrototypeSpawnsStackMaterialTest : GameTest
{
    [SidedDependency(Side.Server)] private SharedMapSystem _sMapSystem = null!;

    [Test]
    public async Task MaterialPrototypeSpawnsStackMaterial()
    {
        await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var allMaterialProtos = SProtoMan.EnumeratePrototypes<MaterialPrototype>();
            var coords = TestMap!.GridCoords;

            using (Assert.EnterMultipleScope())
            {
                foreach (var proto in allMaterialProtos)
                {
                    if (proto.StackEntity == null)
                        continue;

                    var spawned = SEntMan.SpawnEntity(proto.StackEntity, coords);

                    Assert.That(STryComp<StackComponent>(spawned, out var stack),
                        $"{proto.ID} 'stack entity' {proto.StackEntity} does not have the stack component");

                    Assert.That(STryComp<MaterialComponent>(spawned, out _),
                        $"{proto.ID} 'material stack' {proto.StackEntity} does not have the material component");

                    StackPrototype? stackProto = null;
                    Assert.That(stack?.StackTypeId != null && SProtoMan.TryIndex(stack.StackTypeId, out stackProto),
                        $"{proto.ID} material has no stack prototype");

                    if (stackProto != null)
                        Assert.That(proto.StackEntity, Is.EqualTo(stackProto.Spawn.Id));
                }
            }

            _sMapSystem.DeleteMap(TestMap.MapId);
        });
    }
}
