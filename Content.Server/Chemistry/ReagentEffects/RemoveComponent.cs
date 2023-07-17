using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

/// <summary>
/// Removes designated component.
/// You can apply this effect to the same reagent multiple times for multiple components and it'll still work.
/// </summary>
[UsedImplicitly]
public sealed class RemoveComponent : ReagentEffect
{
    /// <summary>
    /// Name of component to remove, as a string
    /// Note: component name shouldn't have the "component" postfix
    /// WRONG: [component: ReplacementAccentComponent]
    /// RIGHT: [component: ReplacementAccent]
    /// </summary>
    [DataField("component")]
    public string Component = String.Empty;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-remove-component", ("chance", Probability));

    public override void Effect(ReagentEffectArgs args)
    {
        var compFactory = IoCManager.Resolve<IComponentFactory>();
        var entityManager = args.EntityManager;
        var uid = args.SolutionEntity;

        if (!compFactory.TryGetRegistration(Component, out var registration, true))
        {
            Logger.Warning("Component '{0}' doesn't exist!", Component);
            return;
        }

        entityManager.RemoveComponent(uid, registration.Type);
    }
}
