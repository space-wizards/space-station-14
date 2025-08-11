using System.Linq;
using Content.Client.Gameplay;
using Content.Shared.Doors.Components;
using Content.Shared.Effects;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Content.Shared.Damage;
using Content.Shared.Doors.Components;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using System.Linq;
using Content.Shared.Physics;
using Robust.Shared.Physics;

namespace Content.Client.Weapons.Melee;

public sealed partial class MeleeWeaponSystem : SharedMeleeWeaponSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    private const string MeleeLungeKey = "melee-lunge";

    public override void Initialize()
    {
        base.Initialize();
        _xformQuery = GetEntityQuery<TransformComponent>();
        SubscribeNetworkEvent<MeleeLungeEvent>(OnMeleeLunge);
        UpdatesOutsidePrediction = true;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        UpdateEffects();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        var entityNull = _player.LocalEntity;

        if (entityNull == null)
            return;

        var entity = entityNull.Value;

        if (!TryGetWeapon(entity, out var weaponUid, out var weapon))
            return;

        if (!CombatMode.IsInCombatMode(entity) || !Blocker.CanAttack(entity, weapon: (weaponUid, weapon)))
        {
            weapon.Attacking = false;
            return;
        }

        var useDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);
        var altDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.UseSecondary);

        if (weapon.AutoAttack || useDown != BoundKeyState.Down && altDown != BoundKeyState.Down)
        {
            if (weapon.Attacking)
            {
                RaisePredictiveEvent(new StopAttackEvent(GetNetEntity(weaponUid)));
            }
        }

        if (weapon.Attacking || weapon.NextAttack > Timing.CurTime)
        {
            return;
        }

        // TODO using targeted actions while combat mode is enabled should NOT trigger attacks.

        var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);

        if (mousePos.MapId == MapId.Nullspace)
        {
            return;
        }

        EntityCoordinates coordinates;

        if (MapManager.TryFindGridAt(mousePos, out var gridUid, out _))
        {
            coordinates = TransformSystem.ToCoordinates(gridUid, mousePos);
        }
        else
        {
            coordinates = TransformSystem.ToCoordinates(_map.GetMap(mousePos.MapId), mousePos);
        }

        // If the gun has AltFireComponent, it can be used to attack.
        if (TryComp<GunComponent>(weaponUid, out var gun) && gun.UseKey)
        {
            if (!TryComp<AltFireMeleeComponent>(weaponUid, out var altFireComponent) || altDown != BoundKeyState.Down)
                return;

            switch(altFireComponent.AttackType)
            {
                case AltFireAttackType.Light:
                    ClientLightAttack(entity, mousePos, coordinates, weaponUid, weapon);
                    break;

                case AltFireAttackType.Heavy:
                    ClientHeavyAttack(entity, coordinates, weaponUid, weapon);
                    break;

                case AltFireAttackType.Disarm:
                    ClientDisarm(entity, mousePos, coordinates);
                    break;
            }

            return;
        }

        // Heavy attack.
        if (altDown == BoundKeyState.Down)
        {
            // If it's an unarmed attack then do a disarm
            if (weapon.AltDisarm && weaponUid == entity)
            {
                ClientDisarm(entity, mousePos, coordinates);
                return;
            }

            ClientHeavyAttack(entity, coordinates, weaponUid, weapon);
            return;
        }

        // Light attack
        if (useDown == BoundKeyState.Down)
            ClientLightAttack(entity, mousePos, coordinates, weaponUid, weapon);
    }

    protected override bool InRange(EntityUid user, EntityUid target, float range, ICommonSession? session)
    {
        var targetXform = Transform(target);
        if (Interaction.InRangeUnobstructed(user, target, targetXform.Coordinates, targetXform.LocalRotation, range, overlapCheck: false))
            return true;

        // --- Systemic Fallback for Same-Tile Obstructions ---
        var userXform = Transform(user);

        var userPos = TransformSystem.GetWorldPosition(userXform);
        var targetPos = TransformSystem.GetWorldPosition(targetXform);
        var distance = (targetPos - userPos).Length();

        if (distance > range)
            return false;

        var mapId = userXform.MapID;
        if (mapId == MapId.Nullspace)
            return false;

        var dir = (targetPos - userPos).Normalized();
        const int attackMask = (int) (CollisionGroup.MobMask | CollisionGroup.Opaque);

        var ray = new CollisionRay(userPos, dir, attackMask);
        var rayCastResults = _physics.IntersectRay(mapId, ray, distance, user, false).ToList();

        if (!rayCastResults.Any() || rayCastResults.First().HitEntity == target)
            return true;

        var hitEntity = rayCastResults.First().HitEntity;

        if (targetXform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var hitXform = Transform(hitEntity);
        if (hitXform.GridUid != gridUid)
            return false;

        var targetTile = _map.CoordinatesToTile(gridUid, grid, targetXform.Coordinates);
        var hitTile = _map.CoordinatesToTile(gridUid, grid, hitXform.Coordinates);

        // If the first obstruction is on the same tile as the target, allow the attack.
        return targetTile == hitTile;
    }

    protected override void DoDamageEffect(List<EntityUid> targets, EntityUid? user, TransformComponent targetXform)
    {
        // Server never sends the event to us for predictiveeevent.
        _color.RaiseEffect(Color.Red, targets, Filter.Local());
    }

    /// <summary>
    /// Raises a heavy attack event with the relevant attacked entities.
    /// This is to avoid lag effecting the client's perspective too much.
    /// </summary>
    private void ClientHeavyAttack(EntityUid user, EntityCoordinates coordinates, EntityUid meleeUid, MeleeWeaponComponent component)
    {
        // Only run on first prediction to avoid the potential raycast entities changing.
        if (!_xformQuery.TryGetComponent(user, out var userXform) ||
            !Timing.IsFirstTimePredicted)
        {
            return;
        }

        var targetMap = TransformSystem.ToMapCoordinates(coordinates);

        if (targetMap.MapId != userXform.MapID)
            return;

        var userPos = TransformSystem.GetWorldPosition(userXform);
        var direction = targetMap.Position - userPos;
        var distance = MathF.Min(component.Range, direction.Length());

        // This should really be improved. GetEntitiesInArc uses pos instead of bounding boxes.
        // Server will validate it with InRangeUnobstructed.
        var entities = GetNetEntityList(ArcRayCast(userPos, direction.ToWorldAngle(), component.Angle, distance, userXform.MapID, user).ToList());
        RaisePredictiveEvent(new HeavyAttackEvent(GetNetEntity(meleeUid), entities.GetRange(0, Math.Min(MaxTargets, entities.Count)), GetNetCoordinates(coordinates)));
    }

    private void ClientDisarm(EntityUid attacker, MapCoordinates mousePos, EntityCoordinates coordinates)
    {
        EntityUid? target = null;

        if (_stateManager.CurrentState is GameplayStateBase screen)
            target = screen.GetClickedEntity(mousePos);

        RaisePredictiveEvent(new DisarmAttackEvent(GetNetEntity(target), GetNetCoordinates(coordinates)));
    }

    private void ClientLightAttack(EntityUid attacker, MapCoordinates mousePos, EntityCoordinates coordinates, EntityUid weaponUid, MeleeWeaponComponent meleeComponent)
    {
        var attackerPos = TransformSystem.GetMapCoordinates(attacker);

        if (mousePos.MapId != attackerPos.MapId || (attackerPos.Position - mousePos.Position).Length() > meleeComponent.Range)
            return;

        // First, get a broad list of candidate entities near the cursor using their physical AABBs
        var candidateEntities = _lookup.GetEntitiesIntersecting(mousePos);
        var eyeRot = _eyeManager.CurrentEye.Rotation;

        // Now, filter this list to only include entities where the cursor is actually over their visible sprite.
        var validTargets = new List<(EntityUid Entity, SpriteComponent Sprite)>();
        foreach (var uid in candidateEntities)
        {
            if (!TryComp<SpriteComponent>(uid, out var sprite))
                continue;

            var xform = Transform(uid);
            var (worldPos, worldRot) = TransformSystem.GetWorldPositionRotation(xform);

            // Calculate the sprite's precise bounds in the world.
            var bounds = _sprite.CalculateBounds((uid, sprite), worldPos, worldRot, eyeRot);

            // Check if the mouse cursor is inside these precise bounds
            if (bounds.Contains(mousePos.Position))
            {
                validTargets.Add((uid, sprite));
            }
        }

        if (validTargets.Count == 0)
        {
            // Send a miss event if we clicked on nothing.
            RaisePredictiveEvent(new LightAttackEvent(null, GetNetEntity(weaponUid), GetNetCoordinates(coordinates)));
            return;
        }

        // Sort the valid targets by priority: Mobs > everything else. Then sort by draw depth
        var sortedEntities = validTargets
            .OrderByDescending(e => HasComp<MobStateComponent>(e.Entity))
            .ThenByDescending(e => e.Sprite.DrawDepth)
            .Select(e => e.Entity)
            .ToList();

        EntityUid? target = null;

        // Find the highest-priority damageable entity from our precisely filtered list.
        foreach (var entity in sortedEntities)
        {
            if (HasComp<DamageableComponent>(entity))
            {
                target = entity;
                break;
            }
        }

        if (target != null)
        {
            // If the found target is the top-most entity based on our new priority sort AND it's interactable,
            // we let the interaction system take priority. This prevents accidentally attacking friendly NPCs.
            if (target == sortedEntities[0] && Interaction.CombatModeCanHandInteract(attacker, target.Value))
            {
                return;
            }
        }
        else
        {
            // No damageable entity was found. Still respect CombatModeCanHandInteract for the top entity.
            if (Interaction.CombatModeCanHandInteract(attacker, sortedEntities[0]))
                return;
        }

        if (target == attacker)
            target = null;

        RaisePredictiveEvent(new LightAttackEvent(GetNetEntity(target), GetNetEntity(weaponUid), GetNetCoordinates(coordinates)));
    }

    private void OnMeleeLunge(MeleeLungeEvent ev)
    {
        var ent = GetEntity(ev.Entity);
        var entWeapon = GetEntity(ev.Weapon);

        // Entity might not have been sent by PVS.
        if (Exists(ent) && Exists(entWeapon))
            DoLunge(ent, entWeapon, ev.Angle, ev.LocalPos, ev.Animation);
    }
}
