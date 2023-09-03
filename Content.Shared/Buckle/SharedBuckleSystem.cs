using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Pulling;
using Content.Shared.Standing;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Players;
using Robust.Shared.Timing;

namespace Content.Shared.Buckle;

public abstract partial class SharedBuckleSystem : EntitySystem
{
    [Dependency] private INetManager _netManager = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private ISharedPlayerManager _playerManager = default!;

    [Dependency] protected ActionBlockerSystem ActionBlockerSystem = default!;
    [Dependency] private   AlertsSystem _alertsSystem = default!;
    [Dependency] private   MobStateSystem _mobStateSystem = default!;
    [Dependency] protected SharedAppearanceSystem AppearanceSystem = default!;
    [Dependency] private   SharedAudioSystem _audioSystem = default!;
    [Dependency] private   SharedContainerSystem _containerSystem = default!;
    [Dependency] private   SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private   SharedJointSystem _joints = default!;
    [Dependency] private   SharedPopupSystem _popupSystem = default!;
    [Dependency] private   SharedPullingSystem _pullingSystem = default!;
    [Dependency] private   SharedTransformSystem _transformSystem = default!;
    [Dependency] private   StandingStateSystem _standingSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(SharedInteractionSystem));
        UpdatesAfter.Add(typeof(SharedInputSystem));

        InitializeBuckle();
        InitializeStrap();
    }

    /// <summary>
    /// Reattaches this entity to the strap, modifying its position and rotation.
    /// </summary>
    /// <param name="buckleUid">The entity to reattach.</param>
    /// <param name="strapUid">The entity to reattach the buckleUid entity to.</param>
    private void ReAttach(
        EntityUid buckleUid,
        EntityUid strapUid,
        BuckleComponent? buckleComp = null,
        StrapComponent? strapComp = null)
    {
        if (!Resolve(strapUid, ref strapComp, false)
            || !Resolve(buckleUid, ref buckleComp, false))
            return;

        var buckleTransform = Transform(buckleUid);

        buckleTransform.Coordinates = new EntityCoordinates(strapUid, strapComp.BuckleOffset);

        // Buckle subscribes to move for <reasons> so this might fail.
        // TODO: Make buckle not do that.
        if (buckleTransform.ParentUid != strapUid)
            return;

        _transformSystem.SetLocalRotation(buckleUid, Angle.Zero, buckleTransform);
        _joints.RefreshRelay(buckleUid, strapUid);

        switch (strapComp.Position)
        {
            case StrapPosition.None:
                break;
            case StrapPosition.Stand:
                _standingSystem.Stand(buckleUid);
                break;
            case StrapPosition.Down:
                _standingSystem.Down(buckleUid, false, false);
                break;
        }
    }
}
