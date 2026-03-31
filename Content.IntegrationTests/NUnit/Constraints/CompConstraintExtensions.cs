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
        public static ResolvableConstraintExpression Comp<T>(IIntegrationInstance instance)
            where T : IComponent
        {
            return new ConstraintExpression().Comp<T>(instance);
        }

        public static ResolvableConstraintExpression Comp(Type t, IIntegrationInstance instance)
        {
            return new ConstraintExpression().Comp(t, instance);
        }
    }

    extension(ConstraintExpression expr)
    {
        public ResolvableConstraintExpression Comp<T>(IIntegrationInstance instance)
            where T : IComponent
        {
            return expr.Append(new CompOperator(typeof(T), instance));
        }

        public ResolvableConstraintExpression Comp(Type t, IIntegrationInstance instance)
        {
            return expr.Append(new CompOperator(t, instance));
        }
    }
}
