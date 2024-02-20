using Content.Shared.Alert;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class AdjustAlert : ReagentEffect
{
    [DataField("alertType", required: true)]
    public AlertType Type;

    [DataField]
    public bool Clear;

    [DataField]
    public bool Cooldown;

    [DataField]
    public float Time;

    //JUSTIFICATION: This just changes some visuals, doesn't need to be documented.
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null;

    public override void Effect(ReagentEffectArgs args)
    {
        var alertSys = EntitySystem.Get<AlertsSystem>();
        if (args.EntityManager.HasComponent<AlertsComponent>(args.SolutionEntity))
        {
            if (Clear)
            {
                alertSys.ClearAlert(args.SolutionEntity, Type);
            }
            else
            {
                (TimeSpan, TimeSpan)? cooldown = null;
                if (Cooldown)
                {
                    var timing = IoCManager.Resolve<IGameTiming>();
                    cooldown = (timing.CurTime, timing.CurTime + TimeSpan.FromSeconds(Time));
                }
                alertSys.ShowAlert(args.SolutionEntity, Type, cooldown: cooldown);
            }
        }
    }
}
