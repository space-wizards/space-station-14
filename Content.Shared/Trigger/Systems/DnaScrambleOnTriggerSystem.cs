using Content.Shared.DetailExaminable;
using Content.Shared.Forensics.Systems;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Preferences;
using Content.Shared.Popups;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Network;

namespace Content.Shared.Trigger.Systems;

public sealed class DnaScrambleOnTriggerSystem : XOnTriggerSystem<DnaScrambleOnTriggerComponent>
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly SharedForensicsSystem _forensics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;

    protected override void OnTrigger(Entity<DnaScrambleOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoid))
            return;

        args.Handled = true;

        // Randomness will mispredict
        // and LoadProfile causes a debug assert on the client at the moment.
        if (_net.IsClient)
            return;

        var newProfile = HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species);
        _humanoidAppearance.LoadProfile(target, newProfile, humanoid);
        _metaData.SetEntityName(target, newProfile.Name, raiseEvents: false); // raising events would update ID card, station record, etc.

        // If the entity has the respective components, then scramble the dna and fingerprint strings.
        _forensics.RandomizeDNA(target);
        _forensics.RandomizeFingerprint(target);

        RemComp<DetailExaminableComponent>(target); // remove MRP+ custom description if one exists
        _identity.QueueIdentityUpdate(target); // manually queue identity update since we don't raise the event

        // Can't use PopupClient or PopupPredicted because the trigger might be unpredicted.
        _popup.PopupEntity(Loc.GetString("scramble-on-trigger-popup"), target, target);

        var ev = new DnaScrambledEvent(target);
        RaiseLocalEvent(target, ref ev, true);
    }
}

/// <summary>
/// Raised after an entity has been DNA Scrambled.
/// Useful for forks that need to run their own updates here.
/// </summary>
/// <param name="flag">The entity that had its DNA scrambled.</param>

[ByRefEvent]
public record struct DnaScrambledEvent(EntityUid Target);
