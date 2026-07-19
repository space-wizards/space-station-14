using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Timing;

namespace Content.Server.Movement.Systems;

public sealed partial class JetpackSystem : SharedJetpackSystem
{
    private static readonly EntityTimerId GasTimer = new("gas");

    [Dependency] private GasTankSystem _gasTank = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActiveJetpackComponent, ComponentStartup>(OnActiveStartup);
        SubscribeLocalEvent<ActiveJetpackComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnActiveStartup(Entity<ActiveJetpackComponent> ent, ref ComponentStartup args)
    {
        _timers.SetTimerAt(ent, GasTimer, ent.Comp.TargetTime);
    }

    protected override bool CanEnable(EntityUid uid, JetpackComponent component)
    {
        return base.CanEnable(uid, component) &&
               TryComp<GasTankComponent>(uid, out var gasTank) &&
               !(gasTank.Air.TotalMoles < component.MoleUsage);
    }

    private void OnTimer(Entity<ActiveJetpackComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != GasTimer ||
            !TryComp<JetpackComponent>(ent, out var comp) ||
            !TryComp<GasTankComponent>(ent, out var gasTankComp))
            return;

        var active = ent.Comp;
        var gasTank = (ent.Owner, gasTankComp);
        active.TargetTime = args.FiredAt + TimeSpan.FromSeconds(active.EffectCooldown);
        _timers.SetTimerAt(ent, GasTimer, active.TargetTime);
        var usedAir = _gasTank.RemoveAir(gasTank, comp.MoleUsage);

        if (usedAir == null)
            return;

        var usedEnoughAir =
            MathHelper.CloseTo(usedAir.TotalMoles, comp.MoleUsage, comp.MoleUsage / 100);

        if (!usedEnoughAir)
            SetEnabled(ent, comp, false);

        _gasTank.UpdateUserInterface(gasTank);
    }
}
