using Content.Shared.Access.Components;
using Content.Shared.Examine;
using Content.Shared.Security.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Security.Systems;

public sealed class SharedGenpopSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GenpopIdCardComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ExpireIdCardComponent, MapInitEvent>(OnTestMapInit);
    }

    // TODO: testing. Don't keep forever.
    private void OnTestMapInit(Entity<ExpireIdCardComponent> ent, ref MapInitEvent args)
    {
        if (TryComp<GenpopIdCardComponent>(ent, out var genpop))
        {
            genpop.StartTime = _timing.CurTime;
            Dirty(ent.Owner, genpop);
        }

        ent.Comp.ExpireTime = _timing.CurTime + TimeSpan.FromSeconds(15);
        Dirty(ent);
    }

    private void OnExamine(Entity<GenpopIdCardComponent> ent, ref ExaminedEvent args)
    {
        // This component holds the contextual data for the sentence end time and other such things.
        if (!TryComp<ExpireIdCardComponent>(ent, out var expireIdCard))
            return;

        if (expireIdCard.Permanent)
        {
            args.PushText(Loc.GetString("genpop-prisoner-id-examine-wait-perm",
                ("crime", ent.Comp.Crime)));
        }
        else
        {
            if (expireIdCard.Expired)
            {
                args.PushText(Loc.GetString("genpop-prisoner-id-examine-served",
                    ("crime", ent.Comp.Crime)));
            }
            else
            {
                var sentence = expireIdCard.ExpireTime - ent.Comp.StartTime;
                var remaining = expireIdCard.ExpireTime - _timing.CurTime;

                args.PushText(Loc.GetString("genpop-prisoner-id-examine-wait",
                    ("minutes", remaining.Minutes),
                    ("seconds", remaining.Seconds),
                    ("sentence", sentence.TotalMinutes),
                    ("crime", ent.Comp.Crime)));
            }
        }
    }
}
