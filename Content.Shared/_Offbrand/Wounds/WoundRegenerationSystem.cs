using Content.Shared.Body.Events;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.Wounds;

public sealed partial class WoundRegenerationSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private WoundableSystem _woundable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoundRegenerationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WoundRegenerationComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<WoundRegenerationComponent, WoundableBodyComponent>();
        while (enumerator.MoveNext(out var uid, out var regeneration, out var woundable))
        {
            if (regeneration.LastUpdate is not { } last || last + regeneration.AdjustedUpdateInterval >= _timing.CurTime)
                continue;

            regeneration.LastUpdate = _timing.CurTime;
            DoUpdate((uid, regeneration, woundable));
            Dirty(uid, regeneration);
        }
    }

    private void OnMapInit(Entity<WoundRegenerationComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.LastUpdate ??= _timing.CurTime;
        Dirty(ent);
    }

    private void OnApplyMetabolicMultiplier(Entity<WoundRegenerationComponent> ent, ref ApplyMetabolicMultiplierEvent args)
    {
        ent.Comp.UpdateIntervalMultiplier = args.Multiplier;
        Dirty(ent);
    }

    private void DoUpdate(Entity<WoundRegenerationComponent, WoundableBodyComponent> ent)
    {
        _woundable.HealWounds((ent, ent), ent.Comp1.Damage, true, true);
    }
}
