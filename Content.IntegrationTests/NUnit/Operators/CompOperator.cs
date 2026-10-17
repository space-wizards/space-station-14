using Content.IntegrationTests.NUnit.Constraints;
using NUnit.Framework.Constraints;
using Robust.UnitTesting;

namespace Content.IntegrationTests.NUnit.Operators;

/// <summary>
///     An operator for use by nunit constraint resolution.
/// </summary>
/// <seealso cref="CompExistsConstraint"/>
public sealed class CompOperator : SelfResolvingOperator
{
    private readonly Type _tComp;
    private readonly IIntegrationInstance _instance;

    public CompOperator(Type tComp, IIntegrationInstance instance)
    {
        _tComp = tComp;
        _instance = instance;

        left_precedence = right_precedence = 1;
    }

    public override void Reduce(ConstraintBuilder.ConstraintStack stack)
    {
        if (RightContext is null or BinaryOperator)
            stack.Push(new CompExistsConstraint(_tComp, _instance));
        else
            stack.Push(new CompConstraint(_tComp, _instance, stack.Pop()));
    }
}
