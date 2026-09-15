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
        /// <inheritdoc cref="extension(ConstraintExpression).LifeStage"/>
        public static LifeStageConstraint LifeStage(EntityLifeStage stage, IIntegrationInstance instance)
        {
            return new LifeStageConstraint(stage, instance);
        }

        /// <inheritdoc cref="extension(ConstraintExpression).PreInit"/>
        public static LifeStageConstraint PreInit(IIntegrationInstance instance)
        {
            return Is.LifeStage(EntityLifeStage.PreInit, instance);
        }

        /// <inheritdoc cref="extension(ConstraintExpression).Initializing"/>
        public static LifeStageConstraint Initializing(IIntegrationInstance instance)
        {
            return Is.LifeStage(EntityLifeStage.Initializing, instance);
        }

        /// <inheritdoc cref="extension(ConstraintExpression).Initialized"/>
        public static LifeStageConstraint Initialized(IIntegrationInstance instance)
        {
            return Is.LifeStage(EntityLifeStage.Initialized, instance);
        }

        /// <inheritdoc cref="extension(ConstraintExpression).MapInitialized"/>
        public static LifeStageConstraint MapInitialized(IIntegrationInstance instance)
        {
            return Is.LifeStage(EntityLifeStage.MapInitialized, instance);
        }

        /// <inheritdoc cref="extension(ConstraintExpression).Terminating"/>
        public static LifeStageConstraint Terminating(IIntegrationInstance instance)
        {
            return Is.LifeStage(EntityLifeStage.Terminating, instance);
        }

        /// <inheritdoc cref="extension(ConstraintExpression).Deleted"/>
        public static LifeStageConstraint Deleted(IIntegrationInstance instance)
        {
            return Is.LifeStage(EntityLifeStage.Deleted, instance);
        }
    }

    extension(ConstraintExpression expr)
    {
        /// <summary>
        /// Returns a new constraint that checks if the entity is in the given lifestage.
        /// </summary>
        /// <param name="stage">The <see cref="EntityLifeStage"/> to check for.</param>
        /// <param name="instance">The <see cref="IIntegrationInstance"/> (i.e. Server or Client) on which to perform the test.</param>
        /// <example>
        /// <code>
        ///     // Assert that the server-sided entity myEntity is MapInitialized.
        ///     Assert.That(myEntity, Is.LifeStage(EntityLifeStage.MapInitialized, Server));
        /// </code>
        /// </example>
        public LifeStageConstraint LifeStage(EntityLifeStage stage, IIntegrationInstance instance)
        {
            var c = new LifeStageConstraint(stage, instance);

            expr.Append(c);

            return c;
        }

        /// <summary>
        /// Returns a new constraint that checks if the entity is in the PreInit lifestage.
        /// </summary>
        /// <param name="instance">The <see cref="IIntegrationInstance"/> (i.e. Server or Client) on which to perform the test.</param>
        /// <example>
        /// <code>
        ///     // Assert that the server-sided entity myEntity has not yet been initialized.
        ///     Assert.That(myEntity, Is.PreInit(Server));
        /// </code>
        /// </example>
        public LifeStageConstraint PreInit(IIntegrationInstance instance)
        {
            return expr.LifeStage(EntityLifeStage.PreInit, instance);
        }

        /// <summary>
        /// Returns a new constraint that checks if the entity is in the Intializing lifestage.
        /// </summary>
        /// <param name="instance">The <see cref="IIntegrationInstance"/> (i.e. Server or Client) on which to perform the test.</param>
        /// <example>
        /// <code>
        ///     // Assert that the server-sided entity myEntity is Initializing.
        ///     Assert.That(myEntity, Is.Initializing(Server));
        /// </code>
        /// </example>
        public LifeStageConstraint Initializing(IIntegrationInstance instance)
        {
            return expr.LifeStage(EntityLifeStage.Initializing, instance);
        }

        /// <summary>
        /// Returns a new constraint that checks if the entity is in the Initialized lifestage.
        /// </summary>
        /// <param name="instance">The <see cref="IIntegrationInstance"/> (i.e. Server or Client) on which to perform the test.</param>
        /// <example>
        /// <code>
        ///     // Assert that the server-sided entity myEntity is Initialized.
        ///     Assert.That(myEntity, Is.Initialized(Server));
        /// </code>
        /// </example>
        public LifeStageConstraint Initialized(IIntegrationInstance instance)
        {
            return expr.LifeStage(EntityLifeStage.Initialized, instance);
        }

        /// <summary>
        /// Returns a new constraint that checks if the entity is in the MapInitialized lifestage.
        /// </summary>
        /// <param name="instance">The <see cref="IIntegrationInstance"/> (i.e. Server or Client) on which to perform the test.</param>
        /// <example>
        /// <code>
        ///     // Assert that the server-sided entity myEntity is MapInitialized.
        ///     Assert.That(myEntity, Is.MapInitialized(Server));
        /// </code>
        /// </example>
        public LifeStageConstraint MapInitialized(IIntegrationInstance instance)
        {
            return expr.LifeStage(EntityLifeStage.MapInitialized, instance);
        }

        /// <summary>
        /// Returns a new constraint that checks if the entity is in the Terminating lifestage.
        /// </summary>
        /// <param name="instance">The <see cref="IIntegrationInstance"/> (i.e. Server or Client) on which to perform the test.</param>
        /// <example>
        /// <code>
        ///     // Assert that the server-sided entity myEntity is Terminating.
        ///     Assert.That(myEntity, Is.Terminating(Server));
        /// </code>
        /// </example>
        public LifeStageConstraint Terminating(IIntegrationInstance instance)
        {
            return expr.LifeStage(EntityLifeStage.Terminating, instance);
        }

        /// <summary>
        /// Returns a new constraint that checks if the entity is in the Deleted lifestage.
        /// </summary>
        /// <param name="instance">The <see cref="IIntegrationInstance"/> (i.e. Server or Client) on which to perform the test.</param>
        /// <example>
        /// <code>
        ///     // Assert that the server-sided entity myEntity is Deleted.
        ///     Assert.That(myEntity, Is.Deleted(Server));
        /// </code>
        /// </example>
        public LifeStageConstraint Deleted(IIntegrationInstance instance)
        {
            return expr.LifeStage(EntityLifeStage.Deleted, instance);
        }
    }
}
