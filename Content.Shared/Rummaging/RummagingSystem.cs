using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.EntityTable;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;


namespace Content.Shared.Rummaging;
public sealed class RummagingSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RummageableComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerb);
        SubscribeLocalEvent<RummageableComponent, RummageDoAfterEvent>(OnDoAfterComplete);
    }

    private void OnGetVerb(Entity<RummageableComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        // if the ent is relootable but not currently lootable, skip adding verbs
        if (!IsCurrentlyLootable(entity))
            return;

        // if the user can't rummage or the entity has already been rummaged, skip adding verbs
        if (!TryComp<CanRummageComponent>(args.User, out var rummaging) || entity.Comp.Looted)
            return;

        // args.Verbs.Add complains if I use args.User directly
        var user = args.User;

        // otherwise, add the verb.
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString(rummaging.RummageVerb),
            Priority = 100, // needs to be the highest-prio alt verb, otherwise it just doesn't show up.
            Act = () =>
            {
                var rummageDuration = entity.Comp.RummageDuration * rummaging.RummageModifier;
                _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, rummageDuration,
                    new RummageDoAfterEvent(), entity.Owner, entity.Owner)
                {
                    BlockDuplicate = true,
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    DistanceThreshold = 2f
                });
            }
        });
    }

    /// <summary>
    /// Runs after the do-after. Handles spawning items from YML-definable loot tables.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="rummageable"></param>
    /// <param name="args"></param>
    private void OnDoAfterComplete(Entity<RummageableComponent> entity, ref RummageDoAfterEvent args)
    {
        // this is mostly here to grab the rummaging component.
        if (!TryComp<CanRummageComponent>(args.User, out var rummaging))
            return;

        if (args.Cancelled || !IsCurrentlyLootable(entity))
            return;

        Dirty(entity.Owner, entity.Comp);
        _audio.PlayPredicted(entity.Comp.Sound, entity.Owner, args.User);

        // allows you to override the user's rummageLoot setting on an entity if you so desire.
        if (entity.Comp.RummageLoot != null)
        {
            var spawn = _entityTable.GetSpawns(entity.Comp.RummageLoot).First();
            if (_net.IsServer)
                Spawn(spawn, Transform(entity.Owner).Coordinates);
        }
        // otherwise, uses the user's settings.
        else if (rummaging.RummageLoot != null)
        {
            var spawn = _entityTable.GetSpawns(rummaging.RummageLoot).First();
            if (_net.IsServer)
                Spawn(spawn, Transform(entity.Owner).Coordinates);
        }

        if (!entity.Comp.Looted)
            entity.Comp.Looted = true;

        // and set the next refresh if the entity is relootable.
        if (entity.Comp.Relootable)
            entity.Comp.NextRelootable = _gameTiming.CurTime + entity.Comp.RelootableCooldown;
    }

    /// Checks if the entity is currently lootable - Does not check if the entity has been looted. 
    private bool IsCurrentlyLootable(Entity<RummageableComponent> entity)
    {
        // if the entity doesn't have relootable, return true. if the entity's relootable cooldown is up, return true. else return false
        return !entity.Comp.Relootable || entity.Comp.NextRelootable < _gameTiming.CurTime;
    }
}
