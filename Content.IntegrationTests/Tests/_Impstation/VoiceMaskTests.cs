using Content.Server.VoiceMask;
using Content.Shared.Chat;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Impstation;

[TestFixture, TestOf(typeof(VoiceMaskSystem))]
public sealed class VoiceMaskTests
{
    [Test]
    public async Task TestVoiceMaskTransform()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();

        var invSys = entMan.System<InventorySystem>();

        await server.WaitAssertion(() =>
        {
            var urist = entMan.Spawn("MobHuman");
            var meta = (MetaDataComponent)entMan.GetComponent(urist, typeof(MetaDataComponent));
            var mask = entMan.Spawn("ClothingMaskGasVoiceChameleon");
            var maskVoice = entMan.GetComponent<VoiceMaskComponent>(mask);
            maskVoice.VoiceMaskName = "Urist McVoicemask";

            // Fire a transform name event
            var nameEv = new TransformSpeakerNameEvent(urist, meta.EntityName);
            entMan.EventBus.RaiseLocalEvent(urist, nameEv, false);

            // Mask has not been equipped, name should be same as entity name
            Assert.That(nameEv.VoiceName, Is.EqualTo(meta.EntityName));

            // Equip the voice mask
            Assert.That(invSys.TryEquip(urist, mask, "head", force: true), Is.True);

            // Fire another transform name event
            nameEv = new TransformSpeakerNameEvent(urist, meta.EntityName);
            entMan.EventBus.RaiseLocalEvent(urist, nameEv, false);

            // Mask is equipped, name should be transformed to mask name
            Assert.That(nameEv.VoiceName, Is.EqualTo(maskVoice.VoiceMaskName));
        });

        await pair.CleanReturnAsync();
    }
}
