#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Server.Stack;
using Content.Shared.Stacks;
using Content.Shared.Materials;
using Content.IntegrationTests.Utility;
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
    private static readonly string[] MaterialPrototypes = GameDataScrounger.PrototypesOfKind<MaterialPrototype>();

    [TestCaseSource(nameof(MaterialPrototypes))]
    [Description($"Checks that a {nameof(MaterialPrototype)} with a defined {nameof(MaterialPrototype.StackEntity)} is configured correctly.")]
    [RunOnSide(Side.Server)]
    public async Task MaterialPrototypeSpawnsStackMaterial(string protoId)
    {
        var proto = SProtoMan.Index<MaterialPrototype>(protoId);
        if (proto.StackEntity is not { } stackEntityId)
            return;

        var stackEntityProto = SProtoMan.Index(stackEntityId);

        stackEntityProto.TryGetComponent<StackComponent>(out var stack, SEntMan.ComponentFactory);
        Assert.That(stack, Is.Not.Null, $"{protoId} 'stack entity' {proto.StackEntity} does not have the {nameof(StackComponent)}");

        stackEntityProto.TryGetComponent<MaterialComponent>(out var material, SEntMan.ComponentFactory);
        Assert.That(material, Is.Not.Null, $"{protoId} 'material stack' {proto.StackEntity} does not have the {nameof(MaterialComponent)}");

        Assert.That(SProtoMan.TryIndex(stack.StackTypeId, out var stackProto),
            $"{protoId} material has no stack prototype");

        Assert.That(proto.StackEntity, Is.EqualTo(stackProto!.Spawn.Id));
    }
}
