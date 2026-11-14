#nullable enable
using NUnit.Framework.Constraints;
using Robust.UnitTesting;

namespace Content.IntegrationTests.NUnit.Constraints;

/// <summary>
///     Constraint for whether a component exists.
/// </summary>
/// <seealso cref="CompConstraintExtensions"/>
public sealed class CompExistsConstraint(Type component, IIntegrationInstance instance) : Constraint
{
    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        if (ConstraintHelpers.TryActualAsEnt(actual, instance, out var ent, out var invalidType))
        {
            return new ConstraintResult(this, actual, instance.EntMan.HasComponent(ent, component));
        }
        if (!invalidType)
        {
            // TActual can be converted to an EntityUid, but is null
            return new ConstraintResult(this, actual, ConstraintStatus.Failure);
        }
        if (ConstraintHelpers.TryActualAsEntityPrototype(actual, instance, out var proto, out invalidType))
        {
            var compName = instance.EntMan.ComponentFactory.GetComponentName(component);
            return new ConstraintResult(this, actual, proto.Components.ContainsKey(compName));
        }
        if (!invalidType)
        {
            // TActual can be converted to an entity prototype, but is null
            return new ConstraintResult(this, actual, ConstraintStatus.Failure);
        }

        throw new NotImplementedException(
            $"The input type {typeof(TActual)} to {nameof(CompExistsConstraint)} is not a supported entity id or entity prototype.");
    }

    public override string Description => $"has the component {component.Name}";
}
