using System.Linq; // Starlight-edit™
using System.Numerics; // Starlight-edit™
using Content.Server.Chat.Systems;
using Content.Server.Movement.Systems;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Effects;
using Content.Shared.Physics; // Starlight-edit™
using Content.Shared.Speech.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Content.Shared.Chat; // Starlight
using Robust.Shared.Physics; // Starlight-edit™
using Robust.Shared.Physics.Systems; // Starlight-edit™

namespace Content.Server.Weapons.Melee;

public sealed class MeleeWeaponSystem : SharedMeleeWeaponSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageExamineSystem _damageExamine = default!;
    [Dependency] private readonly LagCompensationSystem _lag = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedMapSystem _map = default!; // Starlight-edit™
    [Dependency] private readonly SharedPhysicsSystem _physics = default!; // Starlight-edit™

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MeleeSpeechComponent, MeleeHitEvent>(OnSpeechHit);
        SubscribeLocalEvent<MeleeWeaponComponent, DamageExamineEvent>(OnMeleeExamineDamage);
    }

    private void OnMeleeExamineDamage(EntityUid uid, MeleeWeaponComponent component, ref DamageExamineEvent args)
    {
        if (component.Hidden)
            return;

        var damageSpec = GetDamage(uid, args.User, component);

        if (damageSpec.Empty)
            return;

        _damageExamine.AddDamageExamine(args.Message, Damageable.ApplyUniversalAllModifiers(damageSpec), Loc.GetString("damage-melee"));
    }

    protected override bool ArcRaySuccessful(EntityUid targetUid,
        Vector2 position,
        Angle angle,
        Angle arcWidth,
        float range,
        MapId mapId,
        EntityUid ignore,
        ICommonSession? session)
    {
        // Originally the client didn't predict damage effects so you'd intuit some level of how far
        // in the future you'd need to predict, but then there was a lot of complaining like "why would you add artificial delay" as if ping is a choice.
        // Now damage effects are predicted but for wide attacks it differs significantly from client and server so your game could be lying to you on hits.
        // This isn't fair in the slightest because it makes ping a huge advantage and this would be a hidden system.
        // Now the client tells us what they hit and we validate if it's plausible.

        // Even if the client is sending entities they shouldn't be able to hit:
        // A) Wide-damage is split anyway
        // B) We run the same validation we do for click attacks.

        // Could also check the arc though future effort + if they're aimbotting it's not really going to make a difference.

        // (This runs lagcomp internally and is what clickattacks use)
        if (!Interaction.InRangeUnobstructed(ignore, targetUid, range + 0.1f, overlapCheck: false))
            return false;

        // TODO: Check arc though due to the aforementioned aimbot + damage split comments it's less important.
        return true;
    }

    protected override bool InRange(EntityUid user, EntityUid target, float range, ICommonSession? session)
    {
        // Server-side unobstructed check with lag compensation
        if (session is { } pSession)
        {
            var (targetCoordinates, targetLocalAngle) = _lag.GetCoordinatesAngle(target, pSession); // Starlight-edit-begin™
            if (Interaction.InRangeUnobstructed(user, target, targetCoordinates, targetLocalAngle, range, overlapCheck: false))
                return true;
        }
        else
        {
            // Fallback for when no session is provided
            var targetXformSimple = Transform(target);
            if (Interaction.InRangeUnobstructed(user, target, targetXformSimple.Coordinates, targetXformSimple.LocalRotation, range, overlapCheck: false))
                return true; // Starlight-edit-end™
        }

        // Fallback for same-tile obstructions // Starlight-edit-begin™
        var userXform = Transform(user);
        var targetXform = Transform(target);

        var userPos = TransformSystem.GetWorldPosition(userXform);
        var targetPos = TransformSystem.GetWorldPosition(targetXform);
        var delta = targetPos - userPos;
        var distance = delta.Length();

        if (distance > range)
            return false;

        // If distance is near-zero, it's a point-blank attack. The path is definitionally "unobstructed"
        if (distance < 0.001f)
            return true;

        var mapId = userXform.MapID;
        if (mapId == MapId.Nullspace)
            return false;

        var dir = delta.Normalized();
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

        // If the first obstruction is on the same tile as the target, allow the attack
        return targetTile == hitTile; // Starlight-edit-end™
    }

    protected override void DoDamageEffect(List<EntityUid> targets, EntityUid? user, TransformComponent targetXform)
    {
        var filter = Filter.Pvs(targetXform.Coordinates, entityMan: EntityManager).RemoveWhereAttachedEntity(o => o == user);
        _color.RaiseEffect(Color.Red, targets, filter);
    }

    public override void DoLunge(EntityUid user, EntityUid weapon, Angle angle, Vector2 localPos, string? animation, bool predicted = true)
    {
        Filter filter;

        if (predicted)
        {
            filter = Filter.PvsExcept(user, entityManager: EntityManager);
        }
        else
        {
            filter = Filter.Pvs(user, entityManager: EntityManager);
        }

        RaiseNetworkEvent(new MeleeLungeEvent(GetNetEntity(user), GetNetEntity(weapon), angle, localPos, animation), filter);
    }

    private void OnSpeechHit(EntityUid owner, MeleeSpeechComponent comp, MeleeHitEvent args)
    {
        if (!args.IsHit ||
        !args.HitEntities.Any())
        {
            return;
        }

        if (comp.Battlecry != null) //If the battlecry is set to empty, doesn't speak
        {
            _chat.TrySendInGameICMessage(args.User, comp.Battlecry, InGameICChatType.Speak, true, true, checkRadioPrefix: false);  //Speech that isn't sent to chat or adminlogs
        }

    }
}