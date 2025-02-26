using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Wires;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Turrets;

public abstract partial class SharedDeployableTurretSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedWiresSystem _wires = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeployableTurretComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<DeployableTurretComponent, AttemptChangePanelEvent>(OnAttemptChangeWirePanelWire);
        SubscribeLocalEvent<DeployableTurretComponent, GetVerbsEvent<Verb>>(OnGetVerb);
    }

    private void OnGetVerb(Entity<DeployableTurretComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        if (TryComp<AccessReaderComponent>(ent, out var accessReader) && !_accessReader.IsAllowed(args.User, ent, accessReader))
            return;

        var user = args.User;

        var verb = new Verb
        {
            Priority = 1,
            Text = ent.Comp.Enabled ? Loc.GetString("deployable-turret-component-deactivate") : Loc.GetString("deployable-turret-component-activate"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png")),
            Disabled = !HasAmmo(ent),
            Impact = LogImpact.Low,
            Act = () => { TryToggleState(ent, user); }
        };

        args.Verbs.Add(verb);
    }

    private void OnActivate(Entity<DeployableTurretComponent> ent, ref ActivateInWorldEvent args)
    {
        if (TryComp(ent, out UseDelayComponent? useDelay) && !_useDelay.TryResetDelay((ent, useDelay), true))
            return;

        if (TryComp<AccessReaderComponent>(ent, out var reader) && !_accessReader.IsAllowed(args.User, ent, reader))
        {
            _popup.PopupClient(Loc.GetString("deployable-turret-component-access-denied"), ent, args.User);
            _audio.PlayPredicted(ent.Comp.AccessDeniedSound, ent, args.User);

            return;
        }

        TryToggleState(ent, args.User);
    }

    private void OnAttemptChangeWirePanelWire(Entity<DeployableTurretComponent> ent, ref AttemptChangePanelEvent args)
    {
        if (!ent.Comp.Enabled || args.Cancelled)
            return;

        _popup.PopupClient(Loc.GetString("deployable-turret-component-cant-access-wires"), ent, args.User);

        args.Cancelled = true;
    }

    public bool TryToggleState(Entity<DeployableTurretComponent> ent, EntityUid? user = null)
    {
        return TrySetState(ent, !ent.Comp.Enabled, user);
    }

    public bool TrySetState(Entity<DeployableTurretComponent> ent, bool enabled, EntityUid? user = null)
    {
        if (enabled && ent.Comp.Broken)
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("deployable-turret-component-is-broken"), ent, user.Value);

            return false;
        }

        if (enabled && !HasAmmo(ent))
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("deployable-turret-component-no-ammo"), ent, user.Value);

            return false;
        }

        SetState(ent, enabled, user);

        return true;
    }

    protected virtual void SetState(Entity<DeployableTurretComponent> ent, bool enabled, EntityUid? user = null)
    {
        if (ent.Comp.Enabled == enabled)
            return;

        // Close the wires panel UI on activation
        if (enabled && TryComp<WiresPanelComponent>(ent, out var wires))
            _wires.TogglePanel(ent, wires, false);

        // Update status
        ent.Comp.Enabled = enabled;

        // Determine how much time is remaining in the current animation and the one next in queue
        var animTimeRemaining = MathF.Max((float)(ent.Comp.AnimationCompletionTime - _timing.CurTime).TotalSeconds, 0f);
        var animTimeNext = ent.Comp.Enabled ? ent.Comp.DeploymentLength : ent.Comp.RetractionLength;

        ent.Comp.AnimationCompletionTime = _timing.CurTime + TimeSpan.FromSeconds(animTimeNext + animTimeRemaining);

        // Change the turret's damage modifiers
        if (TryComp<DamageableComponent>(ent, out var damageable))
        {
            var damageSetID = ent.Comp.Enabled ? ent.Comp.DeployedDamageModifierSetId : ent.Comp.RetractedDamageModifierSetId;
            _damageable.SetDamageModifierSetId(ent, damageSetID, damageable);
        }

        // Change the turret's fixtures
        if (ent.Comp.DeployedFixture != null &&
            TryComp(ent, out FixturesComponent? fixtures) &&
            fixtures.Fixtures.TryGetValue(ent.Comp.DeployedFixture, out var fixture))
        {
            _physics.SetHard(ent, fixture, ent.Comp.Enabled);
        }

        // Display message
        var msg = ent.Comp.Enabled ? "deployable-turret-component-activating" : "deployable-turret-component-deactivating";
        _popup.PopupClient(Loc.GetString(msg), ent, user);
    }

    public bool HasAmmo(Entity<DeployableTurretComponent> ent)
    {
        if (TryComp<ProjectileBatteryAmmoProviderComponent>(ent, out var projectilebatteryAmmo) &&
            (projectilebatteryAmmo.Shots > 0 || ent.Comp.Powered))
            return true;

        if (TryComp<HitscanBatteryAmmoProviderComponent>(ent, out var hitscanBatteryAmmo) &&
            (hitscanBatteryAmmo.Shots > 0 || ent.Comp.Powered))
            return true;

        if (TryComp<BallisticAmmoProviderComponent>(ent, out var ballisticAmmo) &&
            ballisticAmmo.Count > 0)
            return true;

        return false;
    }
}
