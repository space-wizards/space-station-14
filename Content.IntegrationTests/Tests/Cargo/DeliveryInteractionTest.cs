#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Forensics;
using Content.Shared.Delivery;
using Content.Shared.FingerprintReader;
using Content.Shared.Forensics.Components;

namespace Content.IntegrationTests.Tests.Cargo;

public sealed class DeliveryInteractionTest : InteractionTest
{
    private const string LetterDeliveryProtoId = "LetterDelivery";

    [Test]
    public async Task UnlockAndOpenTest()
    {
        await SpawnTarget(LetterDeliveryProtoId);
        Assert.That(TryComp<DeliveryComponent>(out var deliveryComp), $"{LetterDeliveryProtoId} does not have DeliveryComponent!");
        Assert.That(TryComp<FingerprintReaderComponent>(out var fingerprintReaderComp), $"{LetterDeliveryProtoId} does not have FingerprintReaderComponent!");

        // The test player mob does not have fingerprints, so let's give it some
        FingerprintComponent? fingerprintComp = null;
        await Server.WaitPost(() => fingerprintComp = SEntMan.EnsureComponent<FingerprintComponent>(SEntMan.GetEntity(Player)));
        var forensicsSys = Server.System<ForensicsSystem>();
        forensicsSys.RandomizeFingerprint(SEntMan.GetEntity(Player));

        Assert.That(deliveryComp!.IsLocked, $"{LetterDeliveryProtoId} spawned unlocked.");
        Assert.That(deliveryComp.IsOpened, Is.False, $"{LetterDeliveryProtoId} spawned opened.");

        // Get the delivery into the player's hand
        await Pickup();

        // Remove the player's fingerprint
        var fingerprintReaderSys = Server.System<FingerprintReaderSystem>();
        var fingerprintReaderEnt = (SEntMan.GetEntity(Target)!.Value, fingerprintReaderComp!);
        // Setting no allowed fingerprints authorizes anybody, so we set a bogus one
        fingerprintReaderSys.SetAllowedFingerprints(fingerprintReaderEnt, ["MonkeysOnTypewriters"]);

        // Try to unlock the delivery without an authorized fingerprint
        await UseInHand();
        Assert.That(deliveryComp.IsLocked, "Unlocked without correct fingerprint.");
        Assert.That(deliveryComp.IsOpened, Is.False, "Opened prematurely.");

        // Authorize the player's fingerprint
        fingerprintReaderSys.AddAllowedFingerprint(fingerprintReaderEnt, fingerprintComp!.Fingerprint!);

        // Unlock the delivery with the correct fingerprint
        await UseInHand();
        Assert.That(deliveryComp.IsLocked, Is.False, "Failed to unlock with fingerprint.");
        Assert.That(deliveryComp.IsOpened, Is.False, "Opened prematurely.");

        // Open the unlocked delivery
        await UseInHand();

        Assert.That(deliveryComp.IsOpened, "Failed to open.");
    }
}
