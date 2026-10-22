using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Chemistry.Reaction;

/// <summary>
/// Transforms a reaction's products using the reagents consumed.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class ReactionExtension
{
    /// <summary>
    /// Runs after reactants are removed and before products are added to the solution.
    /// Reactants are read-only.
    /// Products can be modified.
    /// </summary>
    public abstract void React(IReadOnlyList<ReagentQuantity> reactants, List<ReagentQuantity> products);
}
