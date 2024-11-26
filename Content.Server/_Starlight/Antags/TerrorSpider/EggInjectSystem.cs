using Content.Shared._Starlight.Antags.TerrorSpider;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Spider;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Stealth.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Starlight.Antags.TerrorSpider;

public sealed class EggInjectSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EggInjectionEvent>(EggInjection);
        SubscribeLocalEvent<SpiderComponent, EggInjectionDoAfterEvent>(EggInjectionDoAfter);
    }

    private void EggInjectionDoAfter(Entity<SpiderComponent> ent, ref EggInjectionDoAfterEvent args) 
    {
        if (args.Target.HasValue && !HasComp<HasEggHolderComponent>(args.Target.Value))
        {
            EnsureComp<EggHolderComponent>(args.Target.Value);
            EnsureComp<HasEggHolderComponent>(args.Target.Value);
        }
    }

    private void EggInjection(EggInjectionEvent ev)
    {
        if (HasComp<HasEggHolderComponent>(ev.Target))
        {
            _popup.PopupEntity("The target already contains eggs.", ev.Performer);
            return;
        }
        var doAfter = new DoAfterArgs(EntityManager, ev.Performer, TimeSpan.FromSeconds(6), new EggInjectionDoAfterEvent(), ev.Performer, ev.Target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            DistanceThreshold = 1f
        };
        _doAfter.TryStartDoAfter(doAfter);
    }
}
