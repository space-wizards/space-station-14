using Content.Shared.Verbs;
using Content.Shared.DoAfter;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Linguini.Syntax.Ast;

namespace Content.Shared.Rummaging;
public sealed class RummagingSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RummageableComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerb);

        SubscribeLocalEvent<RummageableComponent, RummageDoAfterEvent>(OnDoAfterComplete);
    }

    /// <summary>
    /// Runs on getting the verbs of a rummageable entity, raised on that entity.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="rummageable"></param>
    /// <param name="args"></param>
    private void OnGetVerb(EntityUid uid, RummageableComponent rummageable, GetVerbsEvent<AlternativeVerb> args)
    {
        // if the ent is relootable and the cooldown has passed, reset looted status.
        if (rummageable.Relootable && rummageable.NextRelootable < _gameTiming.CurTime)
            rummageable.Looted = false;

        // if the user can't rummage or the entity has already been rummaged, don't add the verb.
        if (!TryComp<RummagingComponent>(args.User, out var rummaging) || rummageable.Looted)
            return;

        // otherwise, add the verb.
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString(rummaging.RummageVerb),
            Priority = 100, // needs to be the highest-prio alt verb, otherwise it just doesn't show up.
            Act = () =>
            {
                var rummageDuration = rummageable.RummageDuration * rummaging.RummageModifier;
                _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, rummageDuration,
                    new RummageDoAfterEvent(), uid, uid)
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
    private void OnDoAfterComplete(EntityUid uid, RummageableComponent rummageable, RummageDoAfterEvent args)
    {
        // this is mostly here to grab the rummaging component.
        if (!TryComp<RummagingComponent>(args.User, out var rummaging))
            return;

        if (args.Cancelled || rummageable.Looted)
            return;

        rummageable.Looted = true;
        Dirty(uid, rummageable);
        _audio.PlayPredicted(rummageable.Sound, uid, args.User);

        // allows you to override the user's rummageLoot setting on an entity if you so desire.
        if (rummageable.RummageLoot != null)
        {
            var spawn = _prototypeManager.Index<WeightedRandomEntityPrototype>(rummageable.RummageLoot).Pick(_random);
            if (_net.IsServer)
                Spawn(spawn, Transform(uid).Coordinates);
        }
        // otherwise, uses the user's settings.
        else
        {
            var spawn = _prototypeManager.Index<WeightedRandomEntityPrototype>(rummaging.RummageLoot).Pick(_random);
            if (_net.IsServer)
                Spawn(spawn, Transform(uid).Coordinates);
        }

        // and set the next refresh if the ent is relootable.
        if (rummageable.Relootable)
            rummageable.NextRelootable = _gameTiming.CurTime + rummageable.RelootableCooldown;
    }
}

[Serializable, NetSerializable]
public sealed partial class RummageDoAfterEvent : SimpleDoAfterEvent
{

}
