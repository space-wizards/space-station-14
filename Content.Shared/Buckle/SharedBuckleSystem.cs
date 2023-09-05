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
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    [Dependency] protected readonly ActionBlockerSystem ActionBlocker = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPullingSystem _pulling = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

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

        _transform.SetCoordinates(buckleUid, new EntityCoordinates(strapUid, strapComp.BuckleOffset));

        var buckleTransform = Transform(buckleUid);

        // Buckle subscribes to move for <reasons> so this might fail.
        // TODO: Make buckle not do that.
        if (buckleTransform.ParentUid != strapUid)
            return;

        _transform.SetLocalRotation(buckleUid, Angle.Zero, buckleTransform);
        _joints.RefreshRelay(buckleUid, strapUid);

        switch (strapComp.Position)
        {
            case StrapPosition.None:
                break;
            case StrapPosition.Stand:
                _standing.Stand(buckleUid);
                break;
            case StrapPosition.Down:
                _standing.Down(buckleUid, false, false);
                break;
        }
    }
}
