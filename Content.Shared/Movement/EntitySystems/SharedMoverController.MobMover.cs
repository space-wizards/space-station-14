using System.Diagnostics.CodeAnalysis;
using Content.Shared.Audio;
using Content.Shared.CCVar;
using Content.Shared.Friction;
using Content.Shared.Maps;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Pulling.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Movement.EntitySystems;

public abstract partial class SharedMoverController
{
    private const float StepSoundMoveDistanceRunning = 2;
    private const float StepSoundMoveDistanceWalking = 1.5f;

    private const float FootstepVariation = 0f;
    private const float FootstepVolume = 1f;

    /// <summary>
    /// <see cref="CCVars.MinimumFrictionSpeed"/>
    /// </summary>
    private float _minimumFrictionSpeed;

    /// <summary>
    /// <see cref="CCVars.StopSpeed"/>
    /// </summary>
    private float _stopSpeed;

    /// <summary>
    /// <see cref="CCVars.MobAcceleration"/>
    /// </summary>
    private float _mobAcceleration;

    /// <summary>
    /// <see cref="CCVars.MobFriction"/>
    /// </summary>
    private float _frictionVelocity;

    /// <summary>
    /// <see cref="CCVars.MobWeightlessAcceleration"/>
    /// </summary>
    private float _mobWeightlessAcceleration;

    /// <summary>
    /// <see cref="CCVars.MobWeightlessFriction"/>
    /// </summary>
    private float _weightlessFrictionVelocity;

    /// <summary>
    /// <see cref="CCVars.MobWeightlessFrictionNoInput"/>
    /// </summary>
    private float _weightlessFrictionVelocityNoInput;

    /// <summary>
    /// <see cref="CCVars.MobWeightlessModifier"/>
    /// </summary>
    private float _mobWeightlessModifier;

    private bool _relativeMovement;

    /// <summary>
    /// Cache the mob movement calculation to re-use elsewhere.
    /// </summary>
    public Dictionary<EntityUid, bool> UsedMobMovement = new();

    private void InitializeMobMovement()
    {
        SubscribeLocalEvent<MobMoverComponent, ComponentGetState>(OnMobGetState);
        SubscribeLocalEvent<MobMoverComponent, ComponentHandleState>(OnMobHandleState);
        SubscribeLocalEvent<MobMoverComponent, ComponentInit>(OnMobInit);

        // Hello
        _configManager.OnValueChanged(CCVars.RelativeMovement, SetRelativeMovement, true);
        _configManager.OnValueChanged(CCVars.MinimumFrictionSpeed, SetMinimumFrictionSpeed, true);
        _configManager.OnValueChanged(CCVars.MobFriction, SetFrictionVelocity, true);
        _configManager.OnValueChanged(CCVars.MobWeightlessFriction, SetWeightlessFrictionVelocity, true);
        _configManager.OnValueChanged(CCVars.StopSpeed, SetStopSpeed, true);
        _configManager.OnValueChanged(CCVars.MobAcceleration, SetMobAcceleration, true);
        _configManager.OnValueChanged(CCVars.MobWeightlessAcceleration, SetMobWeightlessAcceleration, true);
        _configManager.OnValueChanged(CCVars.MobWeightlessFrictionNoInput, SetWeightlessFrictionNoInput, true);
        _configManager.OnValueChanged(CCVars.MobWeightlessModifier, SetMobWeightlessModifier, true);
        UpdatesBefore.Add(typeof(SharedTileFrictionController));
    }

    private void SetRelativeMovement(bool value) => _relativeMovement = value;
    private void SetMinimumFrictionSpeed(float value) => _minimumFrictionSpeed = value;
    private void SetStopSpeed(float value) => _stopSpeed = value;
    private void SetFrictionVelocity(float value) => _frictionVelocity = value;
    private void SetWeightlessFrictionVelocity(float value) => _weightlessFrictionVelocity = value;
    private void SetMobAcceleration(float value) => _mobAcceleration = value;
    private void SetMobWeightlessAcceleration(float value) => _mobWeightlessAcceleration = value;
    private void SetWeightlessFrictionNoInput(float value) => _weightlessFrictionVelocityNoInput = value;
    private void SetMobWeightlessModifier(float value) => _mobWeightlessModifier = value;

    private void ShutdownMobMovement()
    {
        _configManager.UnsubValueChanged(CCVars.RelativeMovement, SetRelativeMovement);
        _configManager.UnsubValueChanged(CCVars.MinimumFrictionSpeed, SetMinimumFrictionSpeed);
        _configManager.UnsubValueChanged(CCVars.StopSpeed, SetStopSpeed);
        _configManager.UnsubValueChanged(CCVars.MobFriction, SetFrictionVelocity);
        _configManager.UnsubValueChanged(CCVars.MobWeightlessFriction, SetWeightlessFrictionVelocity);
        _configManager.UnsubValueChanged(CCVars.MobAcceleration, SetMobAcceleration);
        _configManager.UnsubValueChanged(CCVars.MobWeightlessAcceleration, SetMobWeightlessAcceleration);
        _configManager.UnsubValueChanged(CCVars.MobWeightlessFrictionNoInput, SetWeightlessFrictionNoInput);
        _configManager.UnsubValueChanged(CCVars.MobWeightlessModifier, SetMobWeightlessModifier);
    }

    public override void UpdateAfterSolve(bool prediction, float frameTime)
    {
        base.UpdateAfterSolve(prediction, frameTime);
        UsedMobMovement.Clear();
    }

    private void OnMobInit(EntityUid uid, MobMoverComponent component, ComponentInit args)
    {
        component.LastGridAngle = Transform(uid).Parent?.WorldRotation ?? new Angle(0);
    }

    private void OnMobHandleState(EntityUid uid, MobMoverComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MobMoverComponentState state) return;
        component.GrabRange = state.GrabRange;
        component.PushStrength = state.PushStrength;
        component.BaseWalkSpeed = state.BaseWalkSpeed;
        component.BaseSprintSpeed = state.BaseSprintSpeed;
        component.WalkSpeedModifier = state.WalkSpeedModifier;
        component.SprintSpeedModifier = state.SprintSpeedModifier;
        component.HeldMoveButtons = state.Buttons;
        component._lastInputTick = GameTick.Zero;
        component._lastInputSubTick = 0;
        component.CanMove = state.CanMove;
    }

    private void OnMobGetState(EntityUid uid, MobMoverComponent component, ref ComponentGetState args)
    {
        args.State = new MobMoverComponentState(
            component.GrabRange,
            component.PushStrength,
            component.BaseWalkSpeed,
            component.BaseSprintSpeed,
            component.WalkSpeedModifier,
            component.SprintSpeedModifier);
    }

    public void RefreshMovementSpeedModifiers(EntityUid uid, MobMoverComponent? move = null)
    {
        if (!Resolve(uid, ref move, false))
            return;

        var ev = new RefreshMovementSpeedModifiersEvent();
        RaiseLocalEvent(uid, ev, false);

        if (move.WalkSpeedModifier.Equals(ev.WalkSpeedModifier) &&
            move.SprintSpeedModifier.Equals(ev.SprintSpeedModifier)) return;

        move.WalkSpeedModifier = ev.WalkSpeedModifier;
        move.SprintSpeedModifier = ev.SprintSpeedModifier;

        Dirty(move);
    }

    private bool TryGetSound(SimpleMoverComponent mover, TransformComponent xform, out float variation, [NotNullWhen(true)] out string? sound)
    {
        sound = null;
        variation = 0f;

        if (mover is not MobMoverComponent mobMover || !CanSound() || !_tags.HasTag(mover.Owner, "FootstepSound")) return false;

        var coordinates = xform.Coordinates;
        var gridId = coordinates.GetGridUid(EntityManager);
        var distanceNeeded = mover.Sprinting ? StepSoundMoveDistanceRunning : StepSoundMoveDistanceWalking;

        // Handle footsteps.
        if (_mapManager.GridExists(gridId))
        {
            // Can happen when teleporting between grids.
            if (!coordinates.TryDistance(EntityManager, mobMover.LastPosition, out var distance) ||
                distance > distanceNeeded)
            {
                mobMover.StepSoundDistance = distanceNeeded;
            }
            else
            {
                mobMover.StepSoundDistance += distance;
            }
        }
        else
        {
            // In space no one can hear you squeak
            return false;
        }

        DebugTools.Assert(gridId != null);
        mobMover.LastPosition = coordinates;

        if (mobMover.StepSoundDistance < distanceNeeded) return false;

        mobMover.StepSoundDistance -= distanceNeeded;

        if (_inventory.TryGetSlotEntity(mover.Owner, "shoes", out var shoes) &&
            EntityManager.TryGetComponent<FootstepModifierComponent>(shoes, out var modifier))
        {
            sound = modifier.SoundCollection.GetSound();
            variation = modifier.Variation;
            return true;
        }

        return TryGetFootstepSound(gridId!.Value, coordinates, out variation, out sound);
    }

    private bool TryGetFootstepSound(EntityUid gridId, EntityCoordinates coordinates, out float variation, [NotNullWhen(true)] out string? sound)
    {
        variation = 0f;
        sound = null;
        var grid = _mapManager.GetGrid(gridId);
        var tile = grid.GetTileRef(coordinates);

        if (tile.IsSpace(_tileDefinitionManager)) return false;

        // If the coordinates have a FootstepModifier component
        // i.e. component that emit sound on footsteps emit that sound
        foreach (var maybeFootstep in grid.GetAnchoredEntities(tile.GridIndices))
        {
            if (EntityManager.TryGetComponent(maybeFootstep, out FootstepModifierComponent? footstep))
            {
                sound = footstep.SoundCollection.GetSound();
                variation = footstep.Variation;
                return true;
            }
        }

        // Walking on a tile.
        var def = (ContentTileDefinition) _tileDefinitionManager[tile.Tile.TypeId];
        sound = def.FootstepSounds?.GetSound();
        variation = FootstepVariation;

        return !string.IsNullOrEmpty(sound);
    }

    [Serializable, NetSerializable]
    protected sealed class MobMoverComponentState : ComponentState
    {
        public float GrabRange;
        public float PushStrength;
        public float BaseWalkSpeed;
        public float BaseSprintSpeed;
        public float WalkSpeedModifier;
        public float SprintSpeedModifier;
        public MoveButtons Buttons { get; }
        public readonly bool CanMove;

        public MobMoverComponentState(
            float grabRange,
            float pushStrength,
            float baseWalkSpeed,
            float baseSprintSpeed,
            float walkSpeedModifier,
            float sprintSpeedModifier)
        {
            GrabRange = grabRange;
            PushStrength = pushStrength;
            BaseWalkSpeed = baseWalkSpeed;
            BaseSprintSpeed = baseSprintSpeed;
            WalkSpeedModifier = walkSpeedModifier;
            SprintSpeedModifier = sprintSpeedModifier;
        }
    }
}
