using System.Numerics;
using Content.Shared.CardboardBox;
using Content.Shared.CardboardBox.Components;
using Content.Shared.Examine;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Robust.Client.GameObjects;

namespace Content.Client.CardboardBox;

public sealed class CardboardBoxSystem : SharedCardboardBoxSystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly EntityQuery<MobStateComponent> _mobStateQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<PlayBoxEffectMessage>(OnBoxEffect);
    }

    private void OnBoxEffect(PlayBoxEffectMessage msg)
    {
        var source = GetEntity(msg.Source);

        if (!TryComp<CardboardBoxComponent>(source, out var box))
            return;

        var xform = Transform(source);
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

            // no effect for non-mobs that have MobMover, such as mechs and vehicles.
            if (!_mobStateQuery.HasComp(mob))
                continue;

            var ent = Spawn(box.Effect, mapPos);

            if (!TryComp(ent, out TransformComponent? entTransform) || !TryComp<SpriteComponent>(ent, out var sprite))
                continue;

            _sprite.SetOffset((ent, sprite), new Vector2(0, 1));
            _transform.SetParent(ent, entTransform, mob);
        }

    }
}
