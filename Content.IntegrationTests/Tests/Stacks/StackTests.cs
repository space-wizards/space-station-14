using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Stack;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Stacks;

[TestFixture]
[TestOf(typeof(StackSystem))]
public sealed class StackTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly StackSystem _sStackSystem = default!;

    private const string StackEnt1 = "StackEnt1";
    private const string StackEnt2 = "StackEnt2";
    private const string StackEnt3 = "StackEnt3";
    private const string StackPrototype = "StackProtodsfsd";
    private const string StackCount1 = "1";
    private const string StackCount2 = "2";
    private const string StackCount30 = "30";

    [TestPrototypes]
    private const string Prototypes =
        @$"
        - type: stack
          id: {StackPrototype}
          spawn: {StackEnt1}
          maxCount: {StackCount30}

        - type: entity
          id: {StackEnt1}
          components:
          - type: Stack
            stackType: {StackPrototype}
            count: {StackCount1}
          - type: Physics
            bodyType: Dynamic
          - type: Fixtures
            fixtures:
              fix1:
                shape:
                  !type:PhysShapeCircle
                    bounds: ""-0.49,-0.49,0.49,0.49""
                layer:
                - Impassable

        - type: entity
          id: {StackEnt2}
          components:
          - type: Stack
            stackType: {StackPrototype}
            count: {StackCount2}
          - type: Physics
            bodyType: Dynamic
          - type: Fixtures
            fixtures:
              fix1:
                shape:
                  !type:PhysShapeCircle
                    bounds: ""-0.49,-0.49,0.49,0.49""
                layer:
                - Impassable

        - type: entity
          id: {StackEnt3}
          components:
          - type: Stack
            stackType: {StackPrototype}
            count: {StackCount30}
          - type: Physics
            bodyType: Dynamic
          - type: Fixtures
            fixtures:
              fix1:
                shape:
                  !type:PhysShapeCircle
                    bounds: ""-0.49,-0.49,0.49,0.49""
                layer:
                - Impassable
        ";


    /// <summary>
    /// Tests for <see cref="SharedStackSystem.SetCount(Entity{StackComponent}, int)"/>.
    /// </summary>
    [Test]
    public async Task SetTest()
    {
        var stack = await Spawn(StackEnt1);

        // Raising the count
        _sStackSystem.SetCount(stack, int.Parse(StackCount2));
        Assert.That(_sStackSystem.GetCount(stack), Is.EqualTo(int.Parse(StackCount2)));

        // Lowering the count
        _sStackSystem.SetCount(stack, int.Parse(StackCount1));
        Assert.That(_sStackSystem.GetCount(stack), Is.EqualTo(int.Parse(StackCount1)));

        // Setting above the max count clamps to max
        _sStackSystem.SetCount(stack, int.Parse(StackCount30) + 1);
        Assert.That(_sStackSystem.GetCount(stack), Is.EqualTo(int.Parse(StackCount30)));

        // Setting to 0 deletes the stack
        _sStackSystem.SetCount(stack, 0);
        await Server.WaitRunTicks(1);
        Assert.That(SEntMan.EntityCount, Is.Zero);
    }

    /// <summary>
    /// Tests that <see cref="SharedStackSystem.MergeStacks"/> functions as expected with small numbers.
    /// </summary>
    [Test]
    public async Task MergeTest()
    {
        var stacks = new HashSet<EntityUid>();
        var spawnCount = int.Parse(StackCount1) + int.Parse(StackCount2);

        await Server.WaitPost(() =>
        {
            stacks =
            [
                SSpawn(StackEnt1),
                SSpawn(StackEnt2),
            ];

            _sStackSystem.MergeStacks(ref stacks);
        });

        // Wait for the queue deletion of the empty stacks
        await Server.WaitRunTicks(1);

        Assert.Multiple(() =>
        {
            // Assert that only one entity was returned
            // And that it has the correct count
            Assert.That(stacks, Has.Count.EqualTo(1));
            Assert.That(_sStackSystem.GetCount(stacks.First()), Is.EqualTo(spawnCount));

            // Assert that the other stack was set to zero and deleted
            Assert.That(SEntMan.EntityCount, Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Tests that <see cref="SharedStackSystem.MergeStacks"/> functions as expected with large numbers.
    /// </summary>
    [Test]
    public async Task MergeOverflowTest()
    {
        var stacks = new HashSet<EntityUid>();
        var spawnCount = int.Parse(StackCount1) + int.Parse(StackCount2) + int.Parse(StackCount30);

        await Server.WaitPost(() =>
        {
             stacks =
             [
                 SSpawn(StackEnt1),
                 SSpawn(StackEnt2),
                 SSpawn(StackEnt3),
             ];

            _sStackSystem.MergeStacks(ref stacks);
        });

        // Wait for the queue deletion of the empty stacks
        await Server.WaitRunTicks(1);

        var count = 0;
        foreach (var stack in stacks)
        {
            count += _sStackSystem.GetCount(stack);
        }

        Assert.Multiple(() =>
        {
            // Assert that both stacks were returned
            // And that the empty stack was deleted
            Assert.That(stacks, Has.Count.EqualTo(2));
            Assert.That(SEntMan.EntityCount, Is.EqualTo(2));

            // Assert we have the same count as what we spawned
            Assert.That(count, Is.EqualTo(spawnCount));
        });
    }

    /// <summary>
    /// Test for <see cref="SharedStackSystem.TryMergeToContacts"/>.
    /// </summary>
    [Test]
    public async Task MergeContactsTest()
    {
        var spawnCount = int.Parse(StackCount1) + int.Parse(StackCount1);

        var map = await Pair.CreateTestMap();
        await Server.WaitIdleAsync();

        var doner = await SpawnAtPosition(StackEnt1, map.GridCoords);
        var receiver = await SpawnAtPosition(StackEnt1, map.GridCoords);

        _sStackSystem.TryMergeToContacts(doner);

        await Server.WaitRunTicks(1);

        Assert.Multiple(() =>
        {
            Assert.That(_sStackSystem.GetCount(receiver), Is.EqualTo(spawnCount));
            Assert.That(SEntMan.EntityExists(doner), Is.False);
        });

        doner = await SpawnAtPosition(StackEnt3, map.GridCoords);

        _sStackSystem.TryMergeToContacts(doner);

        Assert.Multiple(() =>
        {
            Assert.That(_sStackSystem.GetCount(receiver), Is.EqualTo(int.Parse(StackCount30)));
            Assert.That(_sStackSystem.GetCount(doner), Is.EqualTo(spawnCount));
        });
    }
}
