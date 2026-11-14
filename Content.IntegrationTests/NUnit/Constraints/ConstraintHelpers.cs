#nullable enable
using System.Diagnostics.CodeAnalysis;
using Content.IntegrationTests.NUnit.Utilities;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.UnitTesting;

namespace Content.IntegrationTests.NUnit.Constraints;

public static class ConstraintHelpers
{
    /// <summary>
    ///     A constraint implementation helper to convert TActual into an EntityUid.
    /// </summary>
    /// <param name="t">The input value to try to get an entity uid from.</param>
    /// <param name="instance">The integration test instance to resolve the entity from.</param>
    /// <param name="ent">The resulting entity uid.</param>
    /// <param name="invalidType">Whether TActual is recognized to begin with.</param>
    /// <typeparam name="TActual">The type to cast out of.</typeparam>
    /// <returns>true if the <paramref name="t"/> was converted to a not-null EntityUid, otherwise false.</returns>
    public static bool TryActualAsEnt<TActual>(TActual t, IIntegrationInstance instance, [NotNullWhen(true)] out EntityUid? ent, out bool invalidType)
    {
        if (t is EntityUid u)
        {
            ent = u;
            invalidType = false;
            return true;
        }

        if (t is IAsType<EntityUid> asTy)
        {
            ent = asTy.AsType();
            invalidType = false;
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

            invalidType = false;
            return ent is not null;
        }

        if (t is null)
        {
            ent = null;
            invalidType = false;
            return false;
        }

        ent = null;
        invalidType = true; // Dunno what this type is!
        return false;
    }

    /// <summary>
    ///     A constraint implementation helper to convert TActual into an EntityPrototype.
    /// </summary>
    /// <param name="t">The input value to try to get an entity prototype from.</param>
    /// <param name="instance">The integration test instance to resolve the entity prototype from.</param>
    /// <param name="ent">The resulting entity prototype.</param>
    /// <param name="invalidType">Whether TActual is recognized to begin with.</param>
    /// <typeparam name="TActual">The type to cast out of.</typeparam>
    /// <returns>true if the <paramref name="t"/> was converted to a not-null EntityPrototype, otherwise false.</returns>
    public static bool TryActualAsEntityPrototype<TActual>(TActual t, IIntegrationInstance instance, [NotNullWhen(true)] out EntityPrototype? proto, out bool invalidType)
    {
        if (t is EntityPrototype p)
        {
            proto = p;
            invalidType = false;
            return true;
        }

        if (t is EntProtoId ep)
        {
            invalidType = false;
            return instance.ProtoMan.TryIndex(ep, out proto);
        }

        if (t is string str)
        {
            invalidType = false;
            return instance.ProtoMan.TryIndex(str, out proto);
        }

        if (t is null)
        {
            proto = null;
            invalidType = false;
            return false;
        }

        proto = null;
        invalidType = true;
        return false;
    }
}
