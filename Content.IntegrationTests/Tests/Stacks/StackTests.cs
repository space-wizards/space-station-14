using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Stack;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using static Content.IntegrationTests.Tests.Stacks.StackTestPrototypes;

namespace Content.IntegrationTests.Tests.Stacks;

[TestFixture]
[TestOf(typeof(StackSystem))]
public sealed class StackTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly StackSystem _sStackSystem = default!;

    /// <summary>
    /// Tests for <see cref="SharedStackSystem.SetCount(Entity{StackComponent}, int)"/>.
    /// </summary>
    [Test]
    public async Task SetTest()
    {
        var stack = await Spawn(StackEnt1);

        // Raising the count
        _sStackSystem.SetCount((stack, null), Count2);
        Assert.That(_sStackSystem.GetCount(stack), Is.EqualTo(Count2));

        // Lowering the count
        _sStackSystem.SetCount((stack, null), Count1);
        Assert.That(_sStackSystem.GetCount(stack), Is.EqualTo(Count1));

        // Setting above the max count clamps to max
        _sStackSystem.SetCount((stack, null), Count30 + 1);
        Assert.That(_sStackSystem.GetCount(stack), Is.EqualTo(Count30));

        // Setting to 0 deletes the stack
        _sStackSystem.SetCount((stack, null), 0);
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
        var spawnCount = Count1 + Count2;

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
        var spawnCount = Count1 + Count2 +Count30;

        await Server.WaitPost(() =>
        {
             stacks =
             [
                 SSpawn(StackEnt1),
                 SSpawn(StackEnt2),
                 SSpawn(StackEnt30),
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
        var spawnCount = Count1 + Count1;

        var map = await Pair.CreateTestMap();
        await Server.WaitIdleAsync();

        // Spawn two stacks at the same position so they're contacting
        var doner = await SpawnAtPosition(StackEnt1, map.GridCoords);
        var receiver = await SpawnAtPosition(StackEnt1, map.GridCoords);

        _sStackSystem.TryMergeToContacts(doner);

        // Wait for queue deletion
        await Server.WaitRunTicks(1);

        Assert.Multiple(() =>
        {
            // Assert that the receiver has the total count
            // And that the doner was deleted
            Assert.That(_sStackSystem.GetCount(receiver), Is.EqualTo(spawnCount));
            Assert.That(SEntMan.EntityExists(doner), Is.False);
        });

        // Now test for when there's more count than the receiver can hold
        doner = await SpawnAtPosition(StackEnt30, map.GridCoords);
        spawnCount += Count30;

        _sStackSystem.TryMergeToContacts(doner);

        Assert.Multiple(() =>
        {
            // Assert that the receiver is at its maximum count
            // And that the doner has the remainder of the spawned count
            Assert.That(_sStackSystem.GetCount(receiver), Is.EqualTo(Count30));
            Assert.That(_sStackSystem.GetCount(doner), Is.EqualTo(spawnCount - Count30));
        });
    }
}
