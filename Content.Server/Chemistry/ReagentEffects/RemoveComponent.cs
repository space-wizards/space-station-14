using Content.Shared.Chemistry.Reagent;
using Content.Server.Medical;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Robust.Shared.IoC;

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
        => Loc.GetString("reagent-effect-guidebook-remove-component", ("chance", Probability), ("component", Component));

    public override void Effect(ReagentEffectArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.SolutionEntity;
        // TODO: This is hot garbage (allegedly) and probably needs an engine change to not be a POS. Allegedly.
        var _compFactory = IoCManager.Resolve<IComponentFactory>();

        if (!_compFactory.TryGetRegistration(Component, out var registration, true))
        {
            System.Console.WriteLine("component '{0}' doesn't exist", Component);
            return;
        }

        entityManager.RemoveComponent(uid, registration.Type);
    }
}
