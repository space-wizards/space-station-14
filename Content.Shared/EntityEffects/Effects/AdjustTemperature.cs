// using Content.Server.Temperature.Components;
// using Content.Server.Temperature.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class AdjustTemperature : EntityEffect
{
    [DataField]
    public float Amount;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-adjust-temperature",
            ("chance", Probability),
            ("deltasign", MathF.Sign(Amount)),
            ("amount", MathF.Abs(Amount)));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var evt = new ExecuteEntityEffectEvent<AdjustTemperature>(this, args);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref evt);
    }
    // public override void Effect(EntityEffectBaseArgs args)
    // {
    //     if (args.EntityManager.TryGetComponent(args.TargetEntity, out TemperatureComponent? temp))
    //     {
    //         var sys = args.EntityManager.EntitySysManager.GetEntitySystem<TemperatureSystem>();
    //         var amount = Amount;

    //         if (args is EntityEffectReagentArgs reagentArgs)
    //         {
    //             amount *= reagentArgs.Scale.Float();
    //         }

    //         sys.ChangeHeat(args.TargetEntity, amount, true, temp);
    //     }
    // }
}
