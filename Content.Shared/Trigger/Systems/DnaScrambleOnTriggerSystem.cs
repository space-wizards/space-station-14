using Content.Shared.DetailExaminable;
using Content.Shared.Forensics.Systems;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Preferences;
using Content.Shared.Popups;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Network;

namespace Content.Shared.Trigger.Systems;

public sealed class DnaScrambleOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly SharedForensicsSystem _forensics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DnaScrambleOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<DnaScrambleOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoid))
            return;

        args.Handled = true;

        // Randomness will mispredict
        // and LoadProfile causes a debug assert on the client at the moment.
        if (_net.IsClient)
            return;

        var newProfile = HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species);
        _humanoidAppearance.LoadProfile(target.Value, newProfile, humanoid);
        _metaData.SetEntityName(target.Value, newProfile.Name, raiseEvents: false); // raising events would update ID card, station record, etc.

        // If the entity has the respective components, then scramble the dna and fingerprint strings.
        _forensics.RandomizeDNA(target.Value);
        _forensics.RandomizeFingerprint(target.Value);

        RemComp<DetailExaminableComponent>(target.Value); // remove MRP+ custom description if one exists
        _identity.QueueIdentityUpdate(target.Value); // manually queue identity update since we don't raise the event

        // Can't use PopupClient or PopupPredicted because the trigger might be unpredicted.
        _popup.PopupEntity(Loc.GetString("scramble-on-trigger-popup"), target.Value, target.Value);
    }
}
