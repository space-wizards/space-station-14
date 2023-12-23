using Content.Shared.StationRecords;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Access.Components;

/// <summary>
///     Stores access levels necessary to "use" an entity
///     and allows checking if something or somebody is authorized with these access levels.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AccessReaderComponent : Component
{
    /// <summary>
    /// Whether or not the accessreader is enabled.
    /// If not, it will always let people through.
    /// </summary>
    [DataField("enabled")]
    public bool Enabled = true;

    /// <summary>
    ///     The set of tags that will automatically deny an allowed check, if any of them are present.
    /// </summary>
    [DataField("denyTags", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<AccessLevelPrototype>))]
    public HashSet<string> DenyTags = new();

    /// <summary>
    /// List of access groups that grant access to this reader. Only a single matching group is required to gain access.
    /// A group matches if it is a subset of the set being checked against.
    /// </summary>
    [DataField("access")]
    public List<HashSet<string>> AccessLists = new();

    /// <summary>
    /// A list of <see cref="StationRecordKey"/>s that grant access. Only a single matching key is required tp gaim
    /// access.
    /// </summary>
    [DataField("accessKeys")]
    public HashSet<StationRecordKey> AccessKeys = new();

    /// <summary>
    /// If specified, then this access reader will instead pull access requirements from entities contained in the
    /// given container.
    /// </summary>
    /// <remarks>
    /// This effectively causes <see cref="DenyTags"/>, <see cref="AccessLists"/>, and <see cref="AccessKeys"/> to be
    /// ignored, though <see cref="Enabled"/> is still respected. Access is denied if there are no valid entities or
    /// they all deny access.
    /// </remarks>
    [DataField("containerAccessProvider")]
    public string? ContainerAccessProvider;
}

[Serializable, NetSerializable]
public sealed class AccessReaderComponentState : ComponentState
{
    public bool Enabled;

    public HashSet<string> DenyTags;

    public List<HashSet<string>> AccessLists;

    public List<(NetEntity, uint)> AccessKeys;

    public AccessReaderComponentState(bool enabled, HashSet<string> denyTags, List<HashSet<string>> accessLists, List<(NetEntity, uint)> accessKeys)
    {
        Enabled = enabled;
        DenyTags = denyTags;
        AccessLists = accessLists;
        AccessKeys = accessKeys;
    }
}
