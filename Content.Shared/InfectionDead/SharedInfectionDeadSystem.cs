using System.Linq;
using System.Numerics;
using Content.Shared.InfectionDead.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Shared.InfectionDead;

public sealed class SharedInfectionDeadSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly ITileDefinitionManager _tiledef = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    
    private ISawmill _sawmill = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<InfectionDeadComponent, EntityUnpausedEvent>(OnInfectionDeadUnpause);

    }



    private void OnInfectionDeadUnpause(EntityUid uid, InfectionDeadComponent component, ref EntityUnpausedEvent args)
    {
        component.NextDamageTime += args.PausedTime;
        Dirty(component);
    }

    public void DoInfectionDeadDamage(EntityUid uid, InfectionDeadComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!Timing.IsFirstTimePredicted)
            return;

        component.NextDamageTime = Timing.CurTime + component.DamageDuration;


        var ev = new InfectionDeadDamageEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var infectionDeadQuery = EntityQueryEnumerator<InfectionDeadComponent>();
        while (infectionDeadQuery.MoveNext(out var ent, out var infectionDead))
        {
            // if the stability is under the death threshold,
            // update it every second to start killing it slowly.

            if (Timing.CurTime > infectionDead.NextDamageTime)
            {
                DoInfectionDeadDamage(ent, infectionDead);
            }
        }

    }

}
