using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Content.Shared.Popups;
using Content.Shared.Turrets;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.TurretController;

/// <summary>
/// Oversees entities that can change the component values of linked deployable turrets,
/// specifically their armament and access level exemptions, via an associated UI
/// </summary>
public abstract partial class SharedDeployableTurretControllerSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessreader = default!;
    [Dependency] private readonly TurretTargetSettingsSystem _turretTargetingSettings = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Handling of client messages
        SubscribeLocalEvent<DeployableTurretControllerComponent, DeployableTurretArmamentSettingChangedMessage>(OnArmamentSettingChanged);
        SubscribeLocalEvent<DeployableTurretControllerComponent, DeployableTurretExemptAccessLevelChangedMessage>(OnExemptAccessLevelsChanged);
    }

    private void OnArmamentSettingChanged(Entity<DeployableTurretControllerComponent> ent, ref DeployableTurretArmamentSettingChangedMessage args)
    {
        if (IsUserAllowedAccess(ent, args.Actor))
            ChangeArmamentSetting(ent, args.ArmamentState, args.Actor);

        if (_userInterfaceSystem.TryGetOpenUi(ent.Owner, DeployableTurretControllerUiKey.Key, out var bui))
            bui.Update<DeployableTurretControllerBoundInterfaceState>();
    }

    private void OnExemptAccessLevelsChanged(Entity<DeployableTurretControllerComponent> ent, ref DeployableTurretExemptAccessLevelChangedMessage args)
    {
        if (IsUserAllowedAccess(ent, args.Actor))
            ChangeExemptAccessLevels(ent, args.AccessLevels, args.Enabled, args.Actor);

        if (_userInterfaceSystem.TryGetOpenUi(ent.Owner, DeployableTurretControllerUiKey.Key, out var bui))
            bui.Update<DeployableTurretControllerBoundInterfaceState>();
    }

    protected virtual void ChangeArmamentSetting(Entity<DeployableTurretControllerComponent> ent, int armamentState, EntityUid? user = null)
    {
        ent.Comp.ArmamentState = armamentState;
        Dirty(ent);

        _appearance.SetData(ent, TurretControllerVisuals.ControlPanel, armamentState);

        // Linked turrets are updated on the server side
    }

    protected virtual void ChangeExemptAccessLevels(
        Entity<DeployableTurretControllerComponent> ent,
        HashSet<ProtoId<AccessLevelPrototype>> exemptions,
        bool enabled,
        EntityUid? user = null
    )
    {
        // Update the controller
        if (!TryComp<TurretTargetSettingsComponent>(ent, out var targetSettings))
            return;

        var controller = new Entity<TurretTargetSettingsComponent>(ent, targetSettings);

        foreach (var accessLevel in exemptions)
        {
            if (!ent.Comp.AccessLevels.Contains(accessLevel))
                continue;

            _turretTargetingSettings.SetAccessLevelExemption(controller, accessLevel, enabled);
        }

        Dirty(controller);

        // Linked turrets are updated on the server side
    }

    public bool IsUserAllowedAccess(Entity<DeployableTurretControllerComponent> ent, EntityUid user)
    {
        if (_accessreader.IsAllowed(user, ent))
            return true;

        _popup.PopupClient(Loc.GetString("turret-controls-access-denied"), ent, user);
        _audio.PlayPredicted(ent.Comp.AccessDeniedSound, ent, user);

        return false;
    }
}
