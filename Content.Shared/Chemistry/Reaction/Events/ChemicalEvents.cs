using Content.Shared.Chemistry.Components;
using JetBrains.Annotations;

namespace Content.Shared.Chemistry.Reaction.Events;

/// FOR THE LOVE OF GOD AND ALL THAT IS HOLY DO NOT ADD CHEMICALS INTO THE TRIGGERING SOLUTION OR CALL UPDATECHEMICALS
/// FROM INSIDE ANY CHEMICAL EFFECT THIS WILL CAUSE AN INFINITE LOOP AND SET FIRE TO A SMOL-PUPPY ORPHANAGE. SERIOUSLY DON'T DO IT!
/// If you need to convert chemicals, just use a reaction!

/// <summary>
///     Base class for effects that are triggered by solutions. This should not generally be used outside of the chemistry-related
///     systems.
/// </summary>
[MeansImplicitUse]
[ByRefEvent]
[ImplicitDataDefinitionForInheritors]
public abstract partial class ChemicalEffect : HandledEntityEventArgs
{
    public Entity<SolutionComponent> SolutionEntity;

    [DataField]
    public List<ChemicalCondition>? Conditions = null;

    public void RaiseEvent(EntityManager entityMan,
        EntityUid targetEntity,
        Entity<SolutionComponent> solutionEntity,
        bool broadcast = false)
    {
        if (Conditions != null)
        {
            foreach (var condition in Conditions)
            {
                var check = condition.RaiseEvent(entityMan, GetTargetEntity(targetEntity), solutionEntity, broadcast);
                if (!check.Valid)
                    return;
            }
        }

        var ev = CreateInstance();
        ev.SolutionEntity = solutionEntity;
            entityMan.EventBus.RaiseLocalEvent(GetTargetEntity(targetEntity), ref ev, broadcast);
            entityMan.EventBus.RaiseLocalEvent(solutionEntity, ref ev, broadcast);
    }

    protected virtual EntityUid GetTargetEntity(EntityUid oldTarget)
    {
        return oldTarget;
    }

    protected abstract ChemicalEffect CreateInstance();
}

[MeansImplicitUse]
[ByRefEvent]
[ImplicitDataDefinitionForInheritors]
public abstract partial class ChemicalCondition : HandledEntityEventArgs
{
    public Entity<SolutionComponent> SolutionEntity;

    public bool Valid = false;

    public ChemicalCondition RaiseEvent(EntityManager entityMan,
        EntityUid targetEntity,
        Entity<SolutionComponent> solutionEntity,
        bool broadcast = false)
    {
        var ev = CreateInstance();
        ev.SolutionEntity = solutionEntity;
        entityMan.EventBus.RaiseLocalEvent(GetTargetEntity(targetEntity), ref ev, broadcast);
        entityMan.EventBus.RaiseLocalEvent(solutionEntity, ref ev, broadcast);
        return ev;
    }

    protected virtual EntityUid GetTargetEntity(EntityUid oldTarget)
    {
        return oldTarget;
    }

    protected abstract ChemicalCondition CreateInstance();
}
