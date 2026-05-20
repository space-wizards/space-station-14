#nullable enable
using Content.IntegrationTests.NUnit.Operators;
using NUnit.Framework.Constraints;
using Robust.Shared.GameObjects;
using Robust.UnitTesting;

namespace Content.IntegrationTests.NUnit.Constraints;

/// <summary>
///     Provides <see cref="M:Content.IntegrationTests.NUnit.Constraints.CompConstraintExtensions.extension(NUnit.Framework.Has).Comp``1(Robust.UnitTesting.IIntegrationInstance)">Has.Comp&lt;T&gt;(side)</see>,
///     a constraint that allows you to check for the presence of, or operate on, a component.
/// </summary>
/// <example>
/// <code>
///     // Assert that the server sided entity myEntity has ItemComponent on the server.
///     Assert.That(myEntity, Has.Comp&lt;ItemComponent&gt;(Server));
/// </code>
/// </example>
public static class CompConstraintExtensions
{
    extension(Has)
    {
        /// <inheritdoc cref="extension(ConstraintExpression).Comp{T}"/>
        public static ResolvableConstraintExpression Comp<T>(IIntegrationInstance instance)
            where T : IComponent
        {
            return new ConstraintExpression().Comp<T>(instance);
        }

        /// <inheritdoc cref="extension(ConstraintExpression).Comp(Type, IIntegrationInstance)"/>
        public static ResolvableConstraintExpression Comp(Type t, IIntegrationInstance instance)
        {
            return new ConstraintExpression().Comp(t, instance);
        }
    }

    extension(ConstraintExpression expr)
    {
        /// <summary>
        /// Returns a new constraint which will either test for the existence of a <typeparamref name="T"/>
        /// on the entity being tested or apply any following constraint to that component.
        /// </summary>
        /// <typeparam name="T">The component Type to check for.</typeparam>
        /// <param name="instance">The <see cref="IIntegrationInstance"/> (i.e. Server or Client) on which to perform the test.</param>
        /// <example>
        /// <code>
        ///     // Assert that the server-sided entity myEntity has an ItemComponent on the server.
        ///     Assert.That(myEntity, Has.Comp&lt;ItemComponent&gt;(Server));
        ///
        ///     // Assert that the server-sided entity myEntity has an ItemComponent with a Size field equal to "Small"
        ///     Assert.That(myEntity,
        ///         Has
        ///             .Comp&lt;ItemComponent&gt;(Server)
        ///             .Property(nameof(ItemComponent.Size))
        ///             .EqualTo("Small")
        ///     );
        /// </code>
        /// </example>
        public ResolvableConstraintExpression Comp<T>(IIntegrationInstance instance)
            where T : IComponent
        {
            return expr.Append(new CompOperator(typeof(T), instance));
        }

        /// <summary>
        /// Returns a new constraint which will either test for the existence of a component of the specified type
        /// on the entity being tested or apply any following constraint to that component.
        /// </summary>
        /// <param name="t">The Type of the component to check for.</param>
        /// <param name="instance">The <see cref="IIntegrationInstance"/> (i.e. Server or Client) on which to perform the test.</param>
        /// <example>
        /// <code>
        ///     // Assert that the server-sided entity myEntity has an ItemComponent on the server.
        ///     Assert.That(myEntity, Has.Comp(typeof(ItemComponent), Server));
        ///
        ///     // Assert that the server-sided entity myEntity has an ItemComponent with a Size field equal to "Small"
        ///     Assert.That(myEntity,
        ///         Has
        ///             .Comp(typeof(ItemComponent), Server)
        ///             .Property(nameof(ItemComponent.Size))
        ///             .EqualTo("Small")
        ///     );
        /// </code>
        /// </example>
        public ResolvableConstraintExpression Comp(Type t, IIntegrationInstance instance)
        {
            return expr.Append(new CompOperator(t, instance));
        }
    }
}
