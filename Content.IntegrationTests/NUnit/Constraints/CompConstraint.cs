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
        if (ConstraintHelpers.TryActualAsEnt(actual, instance, out var ent, out var invalidType))
        {
            if (!instance.EntMan.TryGetComponent(ent, tComp, out var comp))
                return new ConstraintResult(this, actual, ConstraintStatus.Failure);

            var baseResult = Reflect.InvokeApplyTo(constraint: baseConstraint, tComp, comp);
            return new ConstraintResult(this, baseResult.ActualValue, baseResult.Status);
        }
        if (!invalidType)
        {
            // TActual can be converted to an EntityUid, but is null
            return new ConstraintResult(this, actual, ConstraintStatus.Failure);
        }
        if (ConstraintHelpers.TryActualAsEntityPrototype(actual, instance, out var proto, out invalidType))
        {
            var compName = instance.EntMan.ComponentFactory.GetComponentName(tComp);
            if (!proto.Components.TryGetValue(compName, out var comp))
                return new ConstraintResult(this, actual, ConstraintStatus.Failure);

            var baseResult = Reflect.InvokeApplyTo(constraint: baseConstraint, tComp, comp.Component);
            return new ConstraintResult(this, baseResult.ActualValue, baseResult.Status);
        }

        throw new NotImplementedException(
            $"The input type {typeof(TActual)} to {nameof(CompConstraint)} is not a supported entity id or entity prototype.");
    }
}
