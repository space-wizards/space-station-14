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
using Robust.Shared.Players;
using Robust.Shared.Timing;

namespace Content.Shared.Buckle;

public abstract partial class SharedBuckleSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    [Dependency] protected readonly SharedAppearanceSystem AppearanceSystem = default!;
    [Dependency] protected readonly ActionBlockerSystem ActionBlockerSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPullingSystem _pullingSystem = default!;
    [Dependency] private readonly StandingStateSystem _standingSystem = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

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
