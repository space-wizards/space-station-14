#nullable enable
using System.Diagnostics.CodeAnalysis;
using Content.IntegrationTests.NUnit.Utilities;
using Robust.Shared.GameObjects;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.UnitTesting;

namespace Content.IntegrationTests.NUnit.Constraints;

public static class ConstraintHelpers
{
    /// <summary>
    ///     A constraint implementation helper to convert TActual into an entityuid.
    /// </summary>
    /// <param name="t">The input value to try to get an entity uid from.</param>
    /// <param name="instance">The integration test instance to resolve the entity from.</param>
    /// <param name="ent">The resulting entity uid.</param>
    /// <param name="validType">Whether TActual is recognized to begin with.</param>
    /// <typeparam name="TActual">The type to cast out of.</typeparam>
    public static bool TryActualAsEnt<TActual>(TActual t, IIntegrationInstance instance, [NotNullWhen(true)] out EntityUid? ent, out bool validType)
    {
        if (t is EntityUid u)
        {
            ent = u;
            validType = false;
            return true;
        }

        if (t is IAsType<EntityUid> asTy)
        {
            ent = asTy.AsType();
            validType = false;
            return true;
        }

        if (t is IResolvesToEntity resolvable)
        {
            if (instance is IServerIntegrationInstance)
            {
                ent = resolvable.SEntity;
            }
            else if (instance is IClientIntegrationInstance)
            {
                ent = resolvable.CEntity;
            }
            else
            {
                throw new NotSupportedException($"{t.GetType()} is not a valid kind of IIntegrationInstance");
            }

            validType = false;
            return ent is not null;
        }

        if (t is null)
        {
            ent = null;
            validType = false;
            return false;
        }

        ent = null;
        validType = true; // Dunno what this type is!
        return false;
    }
}
