#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Wires;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.EncryptionKeys;

public sealed class RemoveEncryptionKeys : InteractionTest
{
    private static readonly EntProtoId Headset = "ClothingHeadsetGrey";
    private static readonly EntProtoId TelecomServer = "TelecomServerFilled";
    private static readonly EntProtoId MachineFrame = "MachineFrame";
    private static readonly EntProtoId CommonKey = "EncryptionKeyCommon";
    private static readonly EntProtoId CentComKey = "EncryptionKeyCentCom";
    private static readonly ProtoId<RadioChannelPrototype> CommonChannel = "Common";
    private static readonly ProtoId<RadioChannelPrototype> CentComChannel = "CentCom";

    [Test]
    public async Task HeadsetKeys()
    {
        await SpawnTarget(Headset);
        var comp = Comp<EncryptionKeyHolderComponent>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.EqualTo(1));
            Assert.That(comp.DefaultChannel, Is.EqualTo(CommonChannel));
            Assert.That(comp.Channels, Is.EqualTo([CommonChannel]));
        }

        // Remove the key
        await InteractUsing(Screw);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Is.Empty);
            Assert.That(comp.DefaultChannel, Is.Null);
            Assert.That(comp.Channels, Is.Empty);
        }

        // Check that the key was ejected and not just deleted or something.
        await AssertEntityLookup((CommonKey, 1));

        // Re-insert a key.
        await InteractUsing(CentComKey);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.EqualTo(1));
            Assert.That(comp.DefaultChannel, Is.EqualTo(CentComChannel));
            Assert.That(comp.Channels, Is.EqualTo([CentComChannel]));
        }
    }

    [Test]
    public async Task CommsServerKeys()
    {
        await SpawnTarget(TelecomServer);
        var comp = Comp<EncryptionKeyHolderComponent>();
        var panel = Comp<WiresPanelComponent>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Is.Not.Empty);
            Assert.That(comp.Channels, Is.Not.Empty);
            Assert.That(panel.Open, Is.False);
        }

        // cannot remove keys without opening panel
        await InteractUsing(Pry);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Is.Not.Empty);
            Assert.That(comp.Channels, Is.Not.Empty);
            Assert.That(panel.Open, Is.False);
        }

        // Open panel
        await InteractUsing(Screw);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(panel.Open, Is.True);

            // Keys are still here
            Assert.That(comp.KeyContainer.ContainedEntities, Is.Not.Empty);
            Assert.That(comp.Channels, Is.Not.Empty);
        }

        // Now remove the keys
        await InteractUsing(Pry);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Is.Empty);
            Assert.That(comp.Channels, Is.Empty);
        }

        // Reinsert a key
        await InteractUsing(CentComKey);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.EqualTo(1));
            Assert.That(comp.DefaultChannel, Is.EqualTo(CentComChannel));
            Assert.That(comp.Channels, Is.EqualTo([CentComChannel]));
        }

        // Remove it again
        await InteractUsing(Pry);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Is.Empty);
            Assert.That(comp.Channels, Is.Empty);
        }

        // Prying again will start deconstructing the machine.
        AssertPrototype(TelecomServer);
        await InteractUsing(Pry);
        AssertPrototype(MachineFrame);
    }
}
