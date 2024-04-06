using Content.Shared.Chemistry.Components;

namespace Content.Shared.Chemistry.Reaction.Events;

/// FOR THE LOVE OF GOD AND ALL THAT IS HOLY DO NOT ADD CHEMICALS INTO THE TRIGGERING SOLUTION OR CALL UPDATECHEMICALS
/// FROM INSIDE ANY CHEMICAL EFFECT THIS WILL CAUSE AN INFINITE LOOP AND SET FIRE TO A SMOL-PUPPY ORPHANAGE. SERIOUSLY DON'T DO IT!
/// If you need to convert chemicals, just use a reaction!

/// <summary>
/// Base class for effects that are triggered by chemistry. This should not generally be used outside of the chemistry-related
/// systems.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseChemistryEffect : HandledEntityEventArgs
{
    /// <summary>
    ///     The entity that contains this solution
    /// </summary>
    public EntityUid Target = default;

    public abstract bool CheckCondition();

    public abstract void TriggerEffect();
}

/// <summary>
///     Base class for effects that are triggered by solutions. This should not generally be used outside of the chemistry-related
///     systems.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseSolutionEffect : BaseChemistryEffect
{
    /// <summary>
    /// The entity that contains the solution that raised this event
    /// </summary>
    public Entity<SolutionComponent> SolutionEntity = default!;

    /// <summary>
    /// The solution that raised this event
    /// </summary>
    public Solution Solution => SolutionEntity.Comp.Solution;
}



/// <summary>
///     Base class for effects that are triggered by chemistry. This should not generally be used outside of the chemistry-related
///     systems.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseChemistryCondition : HandledEntityEventArgs
{
    /// <summary>
    ///     The entity that contains this solution
    /// </summary>
    public EntityUid Target = default;
    public abstract bool CheckCondition();
}

/// <summary>
///     Base class for effects that are triggered by solutions. This should not generally be used outside of the chemistry-related
///     systems.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseSolutionCondition : BaseChemistryCondition
{
    /// <summary>
    /// The entity that contains the solution that raised this event
    /// </summary>
    public Entity<SolutionComponent> SolutionEntity = default!;

    /// <summary>
    /// The solution that raised this event
    /// </summary>
    public Solution Solution => SolutionEntity.Comp.Solution;
}
