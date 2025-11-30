using System.Linq;
using Content.Shared.Item;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Shared.Whitelist;

/// <summary>
///     Used to determine whether an entity fits a certain whitelist.
///     Does not whitelist by prototypes, since that is undesirable; you're better off just adding a tag to all
///     entity prototypes that need to be whitelisted, and checking for that.
/// </summary>
/// <remarks>
///     Do not add more conditions like itemsize to the whitelist, this should stay as lightweight as possible!
/// </remarks>
/// <code>
/// whitelist:
///   tags:
///   - Cigarette
///   - FirelockElectronics
///   components:
///   - Buckle
///   - AsteroidRock
///   sizes:
///   - Tiny
///   - Large
/// </code>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class EntityWhitelist : IEquatable<EntityWhitelist>
{
    /// <summary>
    ///     Component names that are allowed in the whitelist.
    /// </summary>
    [DataField(customTypeSerializer: typeof(CustomArraySerializer<string, ComponentNameSerializer>))]
    public string[]? Components;

    /// <summary>
    ///     Item sizes that are allowed in the whitelist.
    /// </summary>
    [DataField]
    public List<ProtoId<ItemSizePrototype>>? Sizes;

    [NonSerialized, Access(typeof(EntityWhitelistSystem))]
    public List<ComponentRegistration>? Registrations;

    /// <summary>
    ///     Tags that are allowed in the whitelist.
    /// </summary>
    [DataField]
    public List<ProtoId<TagPrototype>>? Tags;

    /// <summary>
    ///     If false, an entity only requires one of these components or tags to pass the whitelist. If true, an
    ///     entity requires to have ALL of these components and tags to pass.
    ///     The "Sizes" criteria will ignores this, since an item can only have one size.
    /// </summary>
    [DataField]
    public bool RequireAll;

    public override bool Equals(object? obj)
    {
        if (obj is EntityWhitelist whitelist)
            return Equals(whitelist);
        return false;
    }

    public bool Equals(EntityWhitelist? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;

        if (RequireAll != other.RequireAll) return false;
        if (!NullableSequenceEqual(Components, other.Components)) return false;
        if (!NullableSequenceEqual(Sizes, other.Sizes)) return false;
        if (!NullableSequenceEqual(Tags, other.Tags)) return false;

        return true;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(RequireAll);
        AddNullableSequence(ref hash, Components);
        AddNullableSequence(ref hash, Sizes);
        AddNullableSequence(ref hash, Tags);

        return hash.ToHashCode();
    }

    private static bool NullableSequenceEqual<T>(IEnumerable<T>? a, IEnumerable<T>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.SequenceEqual(b);
    }

    private static void AddNullableSequence<T>(ref HashCode hash, IEnumerable<T>? seq)
    {
        if (seq == null)
            hash.Add(0);
        else
            foreach (var item in seq)
                hash.Add(item);
    }
}
