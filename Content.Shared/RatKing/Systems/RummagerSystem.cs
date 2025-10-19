using Content.Shared.DoAfter;
using Content.Shared.EntityTable;
using Content.Shared.RatKing.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.RatKing.Systems;

public sealed class RummagerSystem : EntitySystem
{
    [Dependency] private readonly EntityTableSystem _entityTable =  default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RummageableComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerb);
        SubscribeLocalEvent<RummageableComponent, RummageDoAfterEvent>(OnDoAfterComplete);
    }

    private void OnGetVerb(Entity<RummageableComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!HasComp<RummagerComponent>(args.User) || ent.Comp.Looted)
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rat-king-rummage-text"),
            Priority = 0,
            Act = () =>
            {
                _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
                    user,
                    ent.Comp.RummageDuration,
                    new RummageDoAfterEvent(),
                    ent,
                    ent)
                {
                    BlockDuplicate = true,
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    DistanceThreshold = 2f
                });
            }
        });
    }

    private void OnDoAfterComplete(Entity<RummageableComponent> ent, ref RummageDoAfterEvent args)
    {
        if (args.Cancelled || ent.Comp.Looted)
            return;

        ent.Comp.Looted = true;
        Dirty(ent, ent.Comp);
        _audio.PlayPredicted(ent.Comp.Sound, ent, args.User);

        if (_net.IsClient)
            return;

        var spawns = _entityTable.GetSpawns(ent.Comp.Table);
        var coordinates = Transform(ent).Coordinates;

        foreach (var spawn in spawns)
        {
            Spawn(spawn, coordinates);
        }
    }
}

/// <summary>
/// DoAfter event for rummaging through a container with RummageableComponent.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class RummageDoAfterEvent : SimpleDoAfterEvent;
