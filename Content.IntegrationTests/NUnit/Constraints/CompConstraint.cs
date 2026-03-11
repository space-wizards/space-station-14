using NUnit.Framework.Constraints;
using NUnit.Framework.Internal;
using Robust.UnitTesting;

namespace Content.IntegrationTests.NUnit.Constraints;

/// <summary>
///     A prefix constraint like <see cref="PropertyConstraint"/>, for entity components.
/// </summary>
/// <seealso cref="CompConstraintExtensions"/>
public sealed class CompConstraint(Type tComp, IIntegrationInstance instance, IConstraint baseConstraint)
    : PrefixConstraint(baseConstraint, $"component {tComp.Name}")
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

        if (!instance.EntMan.TryGetComponent(ent, tComp, out var comp))
            return new ConstraintResult(this, actual, ConstraintStatus.Failure);

        var baseResult = Reflect.InvokeApplyTo(constraint: baseConstraint, tComp, comp);
        return new ConstraintResult(this, baseResult.ActualValue, baseResult.Status);
    }
}
