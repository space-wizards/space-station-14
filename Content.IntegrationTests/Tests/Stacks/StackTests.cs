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

    [Test]
    [Description("Tests for SharedStackSystem.SetCount .")]
    public async Task SetTest()
    {
        var stack = await Spawn(StackEnt1);

        // Raising the count
        await Server.WaitPost(() => _sStackSystem.SetCount((stack, null), 2));
        Assert.That(_sStackSystem.GetCount(stack), Is.EqualTo(2));

        // Lowering the count
        await Server.WaitPost(() =>_sStackSystem.SetCount((stack, null), 1));
        Assert.That(_sStackSystem.GetCount(stack), Is.EqualTo(1));

        // Setting above the max count clamps to max
        await Server.WaitPost(() =>_sStackSystem.SetCount((stack, null), 31));
        Assert.That(_sStackSystem.GetCount(stack), Is.EqualTo(30));

        // Setting to 0 deletes the stack
        await Server.WaitPost(() =>_sStackSystem.SetCount((stack, null), 0));
        await Server.WaitRunTicks(1);
        Assert.That(SEntMan.EntityCount, Is.Zero);
    }

    [Test]
    [Description("Tests that SharedStackSystem.MergeStacks functions as expected with small numbers.")]
    public async Task MergeTest()
    {
        var stacks = new HashSet<EntityUid>();

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

        using (Assert.EnterMultipleScope())
        {
            // Assert that only one entity was returned
            // And that it has the correct count
            Assert.That(stacks, Has.Count.EqualTo(1));
            Assert.That(_sStackSystem.GetCount(stacks.First()), Is.EqualTo(3));

            // Assert that the other stack was set to zero and deleted
            Assert.That(SEntMan.EntityCount, Is.EqualTo(1));
        }
    }

    [Test]
    [Description("Tests that SharedStackSystem.MergeStacks functions as expected with large numbers.")]
    public async Task MergeOverflowTest()
    {
        var stacks = new HashSet<EntityUid>();

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
        await Server.WaitPost(() =>
        {
            foreach (var stack in stacks)
            {
                count += _sStackSystem.GetCount(stack);
            }
        });

        using (Assert.EnterMultipleScope())
        {
            // Assert that both stacks were returned
            // And that the empty stack was deleted
            Assert.That(stacks, Has.Count.EqualTo(2));
            Assert.That(SEntMan.EntityCount, Is.EqualTo(2));

            // Assert we have the same count as what we spawned
            Assert.That(count, Is.EqualTo(33));
        }
    }

    [Test]
    [Description("Test for SharedStackSystem.TryMergeToContacts .")]
    public async Task MergeContactsTest()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitIdleAsync();

        // Spawn two stacks at the same position so they're contacting
        var donor = await SpawnAtPosition(StackEnt1, map.GridCoords);
        var receiver = await SpawnAtPosition(StackEnt1, map.GridCoords);

        _sStackSystem.TryMergeToContacts(donor);

        // Wait for queue deletion
        await Server.WaitRunTicks(1);

        using (Assert.EnterMultipleScope())
        {
            // Assert that the receiver has the total count
            // And that the donor was deleted
            Assert.That(_sStackSystem.GetCount(receiver), Is.EqualTo(2));
            Assert.That(SEntMan.EntityExists(donor), Is.False);
        }

        // Now test for when there's more count than the receiver can hold
        donor = await SpawnAtPosition(StackEnt30, map.GridCoords);

        await Server.WaitPost(() => _sStackSystem.TryMergeToContacts(donor));

        using (Assert.EnterMultipleScope())
        {
            // Assert that the receiver is at its maximum count
            // And that the donor has the remainder of the spawned count
            Assert.That(_sStackSystem.GetCount(receiver), Is.EqualTo(30));
            Assert.That(_sStackSystem.GetCount(donor), Is.EqualTo(2));
        }
    }
}
