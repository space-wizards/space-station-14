using System.Numerics;
using Content.Shared.Body.Components;
using Content.Shared.CardboardBox;
using Content.Shared.CardboardBox.Components;
using Content.Shared.Examine;
using Content.Shared.Movement.Components;
using Robust.Client.GameObjects;

namespace Content.Client.CardboardBox;

public sealed class CardboardBoxSystem : SharedCardboardBoxSystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private EntityQuery<BodyComponent> _bodyQuery;

    public override void Initialize()
    {
        base.Initialize();

        _bodyQuery = GetEntityQuery<BodyComponent>();

        SubscribeNetworkEvent<PlayBoxEffectMessage>(OnBoxEffect);
    }

    private void OnBoxEffect(PlayBoxEffectMessage msg)
    {
        var source = GetEntity(msg.Source);

        if (!TryComp<CardboardBoxComponent>(source, out var box))
            return;

        var xformQuery = GetEntityQuery<TransformComponent>();

        if (!xformQuery.TryGetComponent(source, out var xform))
            return;

        var sourcePos = _transform.GetMapCoordinates(source, xform);

        //Any mob that can move should be surprised?
        //God mind rework needs to come faster so it can just check for mind
        //TODO: Replace with Mind Query when mind rework is in.
        var mobMoverEntities = new List<EntityUid>();
        var mover = GetEntity(msg.Mover);

        //Filter out entities in range to see that they're a mob and add them to the mobMoverEntities hash for faster lookup
        var movers = new HashSet<Entity<MobMoverComponent>>();
        _entityLookup.GetEntitiesInRange(xform.Coordinates, box.Distance, movers);

        foreach (var moverComp in movers)
        {
            var uid = moverComp.Owner;
            if (uid == mover)
                continue;

            mobMoverEntities.Add(uid);
        }

        //Play the effect for the mobs as long as they can see the box and are in range.
        foreach (var mob in mobMoverEntities)
        {
            var mapPos = _transform.GetMapCoordinates(mob);
            if (!_examine.InRangeUnOccluded(sourcePos, mapPos, box.Distance, null))
                continue;

            // no effect for anything too exotic
            if (!_bodyQuery.HasComp(mob))
                continue;

            var ent = Spawn(box.Effect, mapPos);

            if (!xformQuery.TryGetComponent(ent, out var entTransform) || !TryComp<SpriteComponent>(ent, out var sprite))
                continue;

            _sprite.SetOffset((ent, sprite), new Vector2(0, 1));
            _transform.SetParent(ent, entTransform, mob);
        }

    }
}
