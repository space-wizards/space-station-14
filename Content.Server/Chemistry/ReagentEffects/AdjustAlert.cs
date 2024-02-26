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
    /// If true, clears the alert immediately
    /// </summary>
    [DataField]
    public bool Clear;

    /// <summary>
    /// Visually display cooldown progress over the alert icon
    /// </summary>
    [DataField]
    public bool ShowCooldown;

    /// <summary>
    /// Automatically remove the alert at the end of the cooldown
    /// </summary>
    [DataField]
    public bool AutoRemove;

    /// <summary>
    /// The length of the cooldown (in seconds).
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

        if (Clear)
        {
                alertSys.ClearAlert(args.SolutionEntity, Type);
        }
        else
        {
            var timing = IoCManager.Resolve<IGameTiming>();
            (TimeSpan, TimeSpan)? cooldown = null;

            if (ShowCooldown || AutoRemove && Time > 0)
                cooldown = (timing.CurTime, timing.CurTime + TimeSpan.FromSeconds(Time));

            alertSys.ShowAlert(args.SolutionEntity, Type, cooldown: cooldown, autoRemove: AutoRemove, showCooldown: ShowCooldown);
        }

    }
}
