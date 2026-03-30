#nullable enable
using NUnit.Framework.Constraints;
using Robust.Shared.GameObjects;
using Robust.UnitTesting;

namespace Content.IntegrationTests.NUnit.Constraints;

/// <summary>
///     A constraint for an entity's lifestage.
/// </summary>
/// <seealso cref="LifeStageConstraintExtensions"/>
public sealed class LifeStageConstraint(EntityLifeStage stage, IIntegrationInstance instance) : Constraint
{
    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        if (!ConstraintHelpers.TryActualAsEnt(actual, instance, out var ent, out var error))
        {
            if (error)
            {
                throw new NotImplementedException(
                    $"The input type {typeof(TActual)} to {nameof(CompExistsConstraint)} is not a supported entity id.");
            }

            return new ConstraintResult(this, actual, ConstraintStatus.Failure);
        }

        var lifestage = instance.EntMan.GetComponentOrNull<MetaDataComponent>(ent.Value)?.EntityLifeStage;

        return new ConstraintResult(this,
            lifestage,
            lifestage == stage || (lifestage is null && stage is EntityLifeStage.Deleted));
    }

    public override string Description => stage switch
    {
        EntityLifeStage.PreInit => "preinitialized",
        EntityLifeStage.Initializing => "initializing",
        EntityLifeStage.Initialized => "initialized",
        EntityLifeStage.MapInitialized => "map initialized",
        EntityLifeStage.Terminating => "terminating",
        EntityLifeStage.Deleted => "deleted",
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null),
    };
}

/// <summary>
///     Provides constraints for testing if an entity is in the given lifestage.
/// </summary>
/// <example>
/// <code>
///     // Assert that the server sided entity myEntity is MapInitialized.
///     Assert.That(myEntity, Is.MapInitialized(Server));
/// </code>
/// </example>
public static class LifeStageConstraintExtensions
{
    extension(Is)
    {
        public static LifeStageConstraint LifeStage(EntityLifeStage stage, IIntegrationInstance instance)
        {
            return new LifeStageConstraint(stage, instance);
        }

        public static LifeStageConstraint PreInit(IIntegrationInstance instance)
        {
            return Is.LifeStage(EntityLifeStage.PreInit, instance);
        }

        public static LifeStageConstraint Initializing(IIntegrationInstance instance)
        {
            return Is.LifeStage(EntityLifeStage.Initializing, instance);
        }

        public static LifeStageConstraint Initialized(IIntegrationInstance instance)
        {
            return Is.LifeStage(EntityLifeStage.Initialized, instance);
        }

        public static LifeStageConstraint MapInitialized(IIntegrationInstance instance)
        {
            return Is.LifeStage(EntityLifeStage.MapInitialized, instance);
        }

        public static LifeStageConstraint Terminating(IIntegrationInstance instance)
        {
            return Is.LifeStage(EntityLifeStage.Terminating, instance);
        }

        public static LifeStageConstraint Deleted(IIntegrationInstance instance)
        {
            return Is.LifeStage(EntityLifeStage.Deleted, instance);
        }
    }

    extension(ConstraintExpression expr)
    {
        public LifeStageConstraint LifeStage(EntityLifeStage stage, IIntegrationInstance instance)
        {
            var c = new LifeStageConstraint(stage, instance);

            expr.Append(c);

            return c;
        }

        public LifeStageConstraint PreInit(IIntegrationInstance instance)
        {
            return expr.LifeStage(EntityLifeStage.PreInit, instance);
        }

        public LifeStageConstraint Initializing(IIntegrationInstance instance)
        {
            return expr.LifeStage(EntityLifeStage.Initializing, instance);
        }

        public LifeStageConstraint Initialized(IIntegrationInstance instance)
        {
            return expr.LifeStage(EntityLifeStage.Initialized, instance);
        }

        public LifeStageConstraint MapInitialized(IIntegrationInstance instance)
        {
            return expr.LifeStage(EntityLifeStage.MapInitialized, instance);
        }

        public LifeStageConstraint Terminating(IIntegrationInstance instance)
        {
            return expr.LifeStage(EntityLifeStage.Terminating, instance);
        }

        public LifeStageConstraint Deleted(IIntegrationInstance instance)
        {
            return expr.LifeStage(EntityLifeStage.Deleted, instance);
        }
    }
}
