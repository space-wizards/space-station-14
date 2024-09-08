using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Radio.Components;
using Content.Shared.Wires;

namespace Content.IntegrationTests.Tests.EncryptionKeys;

public sealed class RemoveEncryptionKeys : InteractionTest
{
    [Test]
    public async Task HeadsetKeys()
    {
        await SpawnTarget("ClothingHeadsetGrey");
        var comp = Comp<EncryptionKeyHolderComponent>();

        Assert.Multiple(() =>
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.EqualTo(1));
            Assert.That(comp.DefaultChannel, Is.EqualTo("Common"));
            Assert.That(comp.Channels, Has.Count.EqualTo(1));
            Assert.That(comp.Channels.First(), Is.EqualTo("Common"));
        });

        // Remove the key
        await InteractUsing(Screw);
        Assert.Multiple(() =>
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.EqualTo(0));
            Assert.That(comp.DefaultChannel, Is.Null);
            Assert.That(comp.Channels, Has.Count.EqualTo(0));
        });

        // Check that the key was ejected and not just deleted or something.
        await AssertEntityLookup(("EncryptionKeyCommon", 1));

        // Re-insert a key.
        await InteractUsing("EncryptionKeyCentCom");
        Assert.Multiple(() =>
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.EqualTo(1));
            Assert.That(comp.DefaultChannel, Is.EqualTo("CentCom"));
            Assert.That(comp.Channels, Has.Count.EqualTo(1));
            Assert.That(comp.Channels.First(), Is.EqualTo("CentCom"));
        });
    }

    [Test]
    public async Task CommsServerKeys()
    {
        await SpawnTarget("TelecomServerFilled");
        var comp = Comp<EncryptionKeyHolderComponent>();
        var panel = Comp<WiresPanelComponent>();

        Assert.Multiple(() =>
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.GreaterThan(0));
            Assert.That(comp.Channels, Has.Count.GreaterThan(0));
            Assert.That(panel.Open, Is.False);
        });

        // cannot remove keys without opening panel
        await InteractUsing(Pry);
        Assert.Multiple(() =>
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.GreaterThan(0));
            Assert.That(comp.Channels, Has.Count.GreaterThan(0));
            Assert.That(panel.Open, Is.False);
        });

        // Open panel
        await InteractUsing(Screw);
        Assert.Multiple(() =>
        {
            Assert.That(panel.Open, Is.True);

            // Keys are still here
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.GreaterThan(0));
            Assert.That(comp.Channels, Has.Count.GreaterThan(0));
        });

        // Now remove the keys
        await InteractUsing(Pry);
        Assert.Multiple(() =>
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.EqualTo(0));
            Assert.That(comp.Channels, Has.Count.EqualTo(0));
        });

        // Reinsert a key
        await InteractUsing("EncryptionKeyCentCom");
        Assert.Multiple(() =>
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.EqualTo(1));
            Assert.That(comp.DefaultChannel, Is.EqualTo("CentCom"));
            Assert.That(comp.Channels, Has.Count.EqualTo(1));
            Assert.That(comp.Channels.First(), Is.EqualTo("CentCom"));
        });

        // Remove it again
        await InteractUsing(Pry);
        Assert.Multiple(() =>
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.EqualTo(0));
            Assert.That(comp.Channels, Has.Count.EqualTo(0));
        });

        // Prying again will start deconstructing the machine.
        AssertPrototype("TelecomServerFilled");
        await InteractUsing(Pry);
        AssertPrototype("MachineFrame");
    }
}
