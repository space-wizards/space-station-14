using Content.Shared.Chemistry.Reagent;
using Content.Shared.Localizations;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

/// <summary>
/// Removes designated component or components.
/// </summary>
[UsedImplicitly]
public sealed class RemoveComponent : ReagentEffect
{
    /// <summary>
    /// ID of component to remove, as a string
    /// Note: component ID shouldn't have the "component" postfix
    /// </summary>
    [DataField("components")]
    public HashSet<string> Components = new();

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var guidebookComponents = new List<string>();

        foreach (var entry in Components)
        {
            guidebookComponents.Add(entry.ToString());
        }
        return Loc.GetString("reagent-effect-guidebook-remove-component",
            ("chance", Probability),
            ("components", ContentLocalizationManager.FormatList(guidebookComponents)));
    }

    public override void Effect(ReagentEffectArgs args)
    {
        var compFactory = IoCManager.Resolve<IComponentFactory>();
        var entityManager = args.EntityManager;
        var uid = args.SolutionEntity;

        foreach (var entry in Components)
        {
            if (!compFactory.TryGetRegistration(entry, out var registration, true))
            {
                Logger.Warning("Component '{0}' doesn't exist!", entry);
            }
            else
            {
                entityManager.RemoveComponent(uid, registration.Type);
            }
        }
    }
}
