using Content.Shared.Chemistry.Managers;
using Content.Shared.OpaqueId;
using Lidgren.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Reagent;

/// <summary>
/// An opaque ID for a reagent.
/// </summary>
/// <remarks>
/// This struct is not a ref type, and consumers may rely on the fact it does not impose GC pressure.
/// </remarks>
public struct ReagentId : IEquatable<ReagentId>, IOpaqueId
{
    /// <summary>
    /// Underlying opaque ID for the reagent.
    /// </summary>
    /// <remarks>This should NEVER be used by the end user, and you should NEVER rely on it's actual value.</remarks>
    public uint InternalId { get; set; }

    /// <summary>
    /// Constructs an opaque reagent ID from the given prototype.
    /// </summary>
    /// <param name="reagentPrototypeId">The prototype to construct an ID for.</param>
    /// <param name="prototypeManager">Prototype manager, to save an IoC lookup.</param>
    /// <exception cref="NullReferenceException">
    /// If this happens, you're trying to construct a reagent ID well before the game has actually set up the table.
    /// On the client, you should only mess with reagents after connection is complete, and on server, only after ReagentManager is initialized.
    /// </exception>
    public ReagentId(string reagentPrototypeId, IPrototypeManager prototypeManager)
    {
        this = prototypeManager.Index<ReagentPrototype>(reagentPrototypeId).OpaqueId!.Value;
    }

    #region Equality
    /// <inheritdoc/>
    public bool Equals(ReagentId other)
    {
        return InternalId == other.InternalId;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is ReagentId other && Equals(other);
    }

    public static bool operator ==(ReagentId left, ReagentId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ReagentId left, ReagentId right)
    {
        return !(left == right);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return InternalId.GetHashCode();
    }

    #endregion Equality
}
