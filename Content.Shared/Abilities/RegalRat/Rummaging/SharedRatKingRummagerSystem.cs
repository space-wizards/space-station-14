using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.RatKing;

public abstract class SharedRatKingRummagerSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RatKingRummageableComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerb);
        SubscribeLocalEvent<RatKingRummageableComponent, RatKingRummageDoAfterEvent>(OnDoAfterComplete);
    }



    private void OnGetVerb(EntityUid uid, RatKingRummageableComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!HasComp<RatKingRummagerComponent>(args.User) || component.Looted)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rat-king-rummage-text"),
            Priority = 0,
            Act = () =>
            {
                _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.RummageDuration,
                    new RatKingRummageDoAfterEvent(), uid, uid)
                {
                    BlockDuplicate = true,
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    DistanceThreshold = 2f
                });
            }
        });
    }

    private void OnDoAfterComplete(EntityUid uid, RatKingRummageableComponent component, RatKingRummageDoAfterEvent args)
    {
        if (args.Cancelled || component.Looted)
            return;

        component.Looted = true;
        Dirty(uid, component);
        _audio.PlayPredicted(component.Sound, uid, args.User);

        var spawn = PrototypeManager.Index<WeightedRandomEntityPrototype>(component.RummageLoot).Pick(Random);
        if (_net.IsServer)
            Spawn(spawn, Transform(uid).Coordinates);
    }
}

[Serializable, NetSerializable]
public sealed partial class RatKingRummageDoAfterEvent : SimpleDoAfterEvent
{

}
