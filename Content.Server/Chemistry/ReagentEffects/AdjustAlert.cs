using Content.Shared.Alert;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class AdjustAlert : ReagentEffect
{
    /// <summary>
    /// The specific Alert that will be adjusted
    /// </summary>
    [DataField("alertType", required: true)]
    public AlertType Type;

    /// <summary>
    /// If true, the alert is removed after Time seconds. If Time was not specified the alert is removed immediately.
    /// </summary>
    [DataField]
    public bool Clear;

    /// <summary>
    /// Visually display cooldown progress over the alert icon.
    /// </summary>
    [DataField]
    public bool ShowCooldown;

    /// <summary>
    /// The length of the cooldown or delay before removing the alert (in seconds).
    /// </summary>
    [DataField]
    public float Time;

    //JUSTIFICATION: This just changes some visuals, doesn't need to be documented.
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null;

    public override void Effect(ReagentEffectArgs args)
    {
        var alertSys = args.EntityManager.EntitySysManager.GetEntitySystem<AlertsSystem>();
        if (!args.EntityManager.HasComponent<AlertsComponent>(args.SolutionEntity))
            return;

        if (Clear && Time <= 0)
        {
                alertSys.ClearAlert(args.SolutionEntity, Type);
        }
        else
        {
            var timing = IoCManager.Resolve<IGameTiming>();
            (TimeSpan, TimeSpan)? cooldown = null;

            if ((ShowCooldown || Clear) && Time > 0)
                cooldown = (timing.CurTime, timing.CurTime + TimeSpan.FromSeconds(Time));

            alertSys.ShowAlert(args.SolutionEntity, Type, cooldown: cooldown, autoRemove: Clear, showCooldown: ShowCooldown);
        }

    }
}
