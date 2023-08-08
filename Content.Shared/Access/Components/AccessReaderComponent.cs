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
public sealed class AccessReaderComponent : Component
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
    ///     List of access lists to check allowed against. For an access check to pass
    ///     there has to be an access list that is a subset of the access in the checking list.
    /// </summary>
    [DataField("access")]
    public List<HashSet<string>> AccessLists = new();

    /// <summary>
    /// A list of valid stationrecordkeys
    /// </summary>
    [DataField("accessKeys")]
    public HashSet<StationRecordKey> AccessKeys = new();


    /// <summary>
    ///     The name of the container in which additional
    ///     AccessReaderComponents may be found.
    /// </summary>
    [DataField("containerAccessProvider")]
    public string? ContainerAccessProvider = null;
}

[Serializable, NetSerializable]
public sealed class AccessReaderComponentState : ComponentState
{
    public bool Enabled;

    public HashSet<string> DenyTags;

    public List<HashSet<string>> AccessLists;

    public HashSet<StationRecordKey> AccessKeys;

    public AccessReaderComponentState(bool enabled, HashSet<string> denyTags, List<HashSet<string>> accessLists, HashSet<StationRecordKey> accessKeys)
    {
        Enabled = enabled;
        DenyTags = denyTags;
        AccessLists = accessLists;
        AccessKeys = accessKeys;
    }
}
