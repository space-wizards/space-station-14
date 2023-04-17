using System.Linq;
using System.Threading.Tasks;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Radio.Components;
using Content.Shared.Wires;
using NUnit.Framework;

namespace Content.IntegrationTests.Tests.EncryptionKeys;

public sealed class RemoveEncryptionKeys : InteractionTest
{
    [Test]
    public async Task HeadsetKeys()
    {
        await SpawnTarget("ClothingHeadsetGrey");
        var comp = Comp<EncryptionKeyHolderComponent>();

        Assert.That(comp.KeyContainer.ContainedEntities.Count, Is.EqualTo(1));
        Assert.That(comp.DefaultChannel, Is.EqualTo("Common"));
        Assert.That(comp.Channels.Count, Is.EqualTo(1));
        Assert.That(comp.Channels.First(), Is.EqualTo("Common"));

        // Remove the key
        await Interact(Screw);
        Assert.That(comp.KeyContainer.ContainedEntities.Count, Is.EqualTo(0));
        Assert.IsNull(comp.DefaultChannel);
        Assert.That(comp.Channels.Count, Is.EqualTo(0));

        // Checkl that the key was ejected and not just deleted or something.
        await AssertEntityLookup(("EncryptionKeyCommon", 1));

        // Re-insert a key.
        await Interact("EncryptionKeyCentCom");
        Assert.That(comp.KeyContainer.ContainedEntities.Count, Is.EqualTo(1));
        Assert.That(comp.DefaultChannel, Is.EqualTo("CentCom"));
        Assert.That(comp.Channels.Count, Is.EqualTo(1));
        Assert.That(comp.Channels.First(), Is.EqualTo("CentCom"));
    }

    [Test]
    public async Task CommsServerKeys()
    {
        await SpawnTarget("TelecomServerFilled");
        var comp = Comp<EncryptionKeyHolderComponent>();
        var panel = Comp<WiresPanelComponent>();

        Assert.That(comp.KeyContainer.ContainedEntities.Count, Is.GreaterThan(0));
        Assert.That(comp.Channels.Count, Is.GreaterThan(0));
        Assert.That(panel.Open, Is.False);

        // cannot remove keys without opening panel
        await Interact(Pry);
        Assert.That(comp.KeyContainer.ContainedEntities.Count, Is.GreaterThan(0));
        Assert.That(comp.Channels.Count, Is.GreaterThan(0));
        Assert.That(panel.Open, Is.False);

        // Open panel
        await Interact(Screw);
        Assert.That(panel.Open, Is.True);

        // Keys are still here
        Assert.That(comp.KeyContainer.ContainedEntities.Count, Is.GreaterThan(0));
        Assert.That(comp.Channels.Count, Is.GreaterThan(0));

        // Now remove the keys
        await Interact(Pry);
        Assert.That(comp.KeyContainer.ContainedEntities.Count, Is.EqualTo(0));
        Assert.That(comp.Channels.Count, Is.EqualTo(0));

        // Reinsert a key
        await Interact("EncryptionKeyCentCom");
        Assert.That(comp.KeyContainer.ContainedEntities.Count, Is.EqualTo(1));
        Assert.That(comp.DefaultChannel, Is.EqualTo("CentCom"));
        Assert.That(comp.Channels.Count, Is.EqualTo(1));
        Assert.That(comp.Channels.First(), Is.EqualTo("CentCom"));

        // Remove it again
        await Interact(Pry);
        Assert.That(comp.KeyContainer.ContainedEntities.Count, Is.EqualTo(0));
        Assert.That(comp.Channels.Count, Is.EqualTo(0));

        // Prying again will start deconstructing the machine.
        AssertPrototype("TelecomServerFilled");
        await Interact(Pry);
        AssertPrototype("MachineFrame");
    }
}

