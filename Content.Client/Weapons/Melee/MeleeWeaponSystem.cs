using System.Linq;
using Content.Client.Gameplay;
using Content.Shared.CombatMode;
using Content.Shared.Hands.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Weapons.Melee;

public sealed partial class MeleeWeaponSystem : SharedMeleeWeaponSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;

    private const string MeleeLungeKey = "melee-lunge";

    public override void Initialize()
    {
        base.Initialize();
        InitializeEffect();
        _overlayManager.AddOverlay(new MeleeWindupOverlay(EntityManager, _timing, _player, _protoManager));
        SubscribeAllEvent<DamageEffectEvent>(OnDamageEffect);
        SubscribeNetworkEvent<MeleeLungeEvent>(OnMeleeLunge);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayManager.RemoveOverlay<MeleeWindupOverlay>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        var entityNull = _player.LocalPlayer?.ControlledEntity;

        if (entityNull == null)
            return;

        var entity = entityNull.Value;

        if (!TryGetWeapon(entity, out var weaponUid, out var weapon))
            return;

        if (!CombatMode.IsInCombatMode(entity) || !Blocker.CanAttack(entity))
        {
            weapon.Attacking = false;
            if (weapon.WindUpStart != null)
            {
                EntityManager.RaisePredictiveEvent(new StopHeavyAttackEvent(weaponUid));
            }

            return;
        }

        // TODO using targeted actions while combat mode is enabled should NOT trigger attacks.

        var useDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);
        var altDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.UseSecondary);
        var currentTime = Timing.CurTime;

        // Heavy attack.
        if (altDown == BoundKeyState.Down)
        {
            // We did the click to end the attack but haven't pulled the key up.
            if (weapon.Attacking)
            {
                return;
            }

            // If it's an unarmed attack then do a disarm
            if (weaponUid == entity)
            {
                EntityUid? target = null;

                var mousePos = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);
                EntityCoordinates coordinates;

                if (MapManager.TryFindGridAt(mousePos, out var grid))
                {
                    coordinates = EntityCoordinates.FromMap(grid.Owner, mousePos, TransformSystem, EntityManager);
                }
                else
                {
                    coordinates = EntityCoordinates.FromMap(MapManager.GetMapEntityId(mousePos.MapId), mousePos, TransformSystem, EntityManager);
                }

                if (_stateManager.CurrentState is GameplayStateBase screen)
                {
                    target = screen.GetClickedEntity(mousePos);
                }

                EntityManager.RaisePredictiveEvent(new DisarmAttackEvent(target, coordinates));
                return;
            }

            // Otherwise do heavy attack if it's a weapon.

            // Start a windup
            if (weapon.WindUpStart == null)
            {
                EntityManager.RaisePredictiveEvent(new StartHeavyAttackEvent(weaponUid));
                weapon.WindUpStart = currentTime;
            }

            // Try to do a heavy attack.
            if (useDown == BoundKeyState.Down)
            {
                var mousePos = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);
                EntityCoordinates coordinates;

                // Bro why would I want a ternary here
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (MapManager.TryFindGridAt(mousePos, out var grid))
                {
                    coordinates = EntityCoordinates.FromMap(grid.Owner, mousePos, TransformSystem, EntityManager);
                }
                else
                {
                    coordinates = EntityCoordinates.FromMap(MapManager.GetMapEntityId(mousePos.MapId), mousePos, TransformSystem, EntityManager);
                }

                ClientHeavyAttack(entity, coordinates, weaponUid, weapon);
            }

            return;
        }

        if (weapon.WindUpStart != null)
        {
            EntityManager.RaisePredictiveEvent(new StopHeavyAttackEvent(weaponUid));
        }

        // Light attack
        if (useDown == BoundKeyState.Down)
        {
            if (weapon.Attacking || weapon.NextAttack > Timing.CurTime)
            {
                return;
            }

            var mousePos = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);
            var attackerPos = Transform(entity).MapPosition;

            if (mousePos.MapId != attackerPos.MapId ||
                (attackerPos.Position - mousePos.Position).Length > weapon.Range)
            {
                return;
            }

            EntityCoordinates coordinates;

            // Bro why would I want a ternary here
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (MapManager.TryFindGridAt(mousePos, out var grid))
            {
                coordinates = EntityCoordinates.FromMap(grid.Owner, mousePos, TransformSystem, EntityManager);
            }
            else
            {
                coordinates = EntityCoordinates.FromMap(MapManager.GetMapEntityId(mousePos.MapId), mousePos, TransformSystem, EntityManager);
            }

            EntityUid? target = null;

            // TODO: UI Refactor update I assume
            if (_stateManager.CurrentState is GameplayStateBase screen)
            {
                target = screen.GetClickedEntity(mousePos);
            }

            RaisePredictiveEvent(new LightAttackEvent(target, weaponUid, coordinates));
            return;
        }

        if (weapon.Attacking)
        {
            RaisePredictiveEvent(new StopAttackEvent(weaponUid));
        }
    }

    protected override bool InRange(EntityUid user, EntityUid target, float range, ICommonSession? session)
    {
        var xform = Transform(target);
        var targetCoordinates = xform.Coordinates;
        var targetLocalAngle = xform.LocalRotation;

        return Interaction.InRangeUnobstructed(user, target, targetCoordinates, targetLocalAngle, range);
    }

    protected override void DoDamageEffect(List<EntityUid> targets, EntityUid? user, TransformComponent targetXform)
    {
        // Server never sends the event to us for predictiveeevent.
        if (_timing.IsFirstTimePredicted)
            RaiseLocalEvent(new DamageEffectEvent(Color.Red, targets));
    }

    protected override bool DoDisarm(EntityUid user, DisarmAttackEvent ev, EntityUid meleeUid, MeleeWeaponComponent component, ICommonSession? session)
    {
        if (!base.DoDisarm(user, ev, meleeUid, component, session))
            return false;

        if (!TryComp<CombatModeComponent>(user, out var combatMode) ||
            combatMode.CanDisarm != true)
        {
            return false;
        }

        // They need to either have hands...
        if (!HasComp<HandsComponent>(ev.Target!.Value))
        {
            // or just be able to be shoved over.
            if (TryComp<StatusEffectsComponent>(ev.Target!.Value, out var status) && status.AllowedEffects.Contains("KnockedDown"))
                return true;

            if (Timing.IsFirstTimePredicted && HasComp<MobStateComponent>(ev.Target.Value))
                PopupSystem.PopupEntity(Loc.GetString("disarm-action-disarmable", ("targetName", ev.Target.Value)), ev.Target.Value);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Raises a heavy attack event with the relevant attacked entities.
    /// This is to avoid lag effecting the client's perspective too much.
    /// </summary>
    private void ClientHeavyAttack(EntityUid user, EntityCoordinates coordinates, EntityUid meleeUid, MeleeWeaponComponent component)
    {
        // Only run on first prediction to avoid the potential raycast entities changing.
        if (!TryComp<TransformComponent>(user, out var userXform) ||
            !Timing.IsFirstTimePredicted)
        {
            return;
        }

        var targetMap = coordinates.ToMap(EntityManager, TransformSystem);

        if (targetMap.MapId != userXform.MapID)
            return;

        var userPos = TransformSystem.GetWorldPosition(userXform);
        var direction = targetMap.Position - userPos;
        var distance = Math.Min(component.Range, direction.Length);

        // This should really be improved. GetEntitiesInArc uses pos instead of bounding boxes.
        // Server will validate it with InRangeUnobstructed.
        var entities = ArcRayCast(userPos, direction.ToWorldAngle(), component.Angle, distance, userXform.MapID, user).ToList();
        RaisePredictiveEvent(new HeavyAttackEvent(meleeUid, entities.GetRange(0, Math.Min(MaxTargets, entities.Count)), coordinates));
    }

    protected override void Popup(string message, EntityUid? uid, EntityUid? user)
    {
        if (!Timing.IsFirstTimePredicted || uid == null)
            return;

        PopupSystem.PopupEntity(message, uid.Value);
    }

    private void OnMeleeLunge(MeleeLungeEvent ev)
    {
        // Entity might not have been sent by PVS.
        if (Exists(ev.Entity))
            DoLunge(ev.Entity, ev.Angle, ev.LocalPos, ev.Animation);
    }
}
