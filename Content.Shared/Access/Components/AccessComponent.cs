using Content.Shared.Access.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Access.Components;

/// <summary>
///     Simple mutable access provider found on ID cards and such.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedAccessSystem))]
[AutoGenerateComponentState]
public sealed partial class AccessComponent : Component
{
    /// <summary>
    /// True if the access provider is enabled and can grant access.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool Enabled = true;

    [DataField]
    [Access(typeof(SharedAccessSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
    [AutoNetworkedField]
    public HashSet<ProtoId<AccessLevelPrototype>> Tags = new();

    /// <summary>
    /// Access Groups. These are added to the tags during map init. After map init this will have no effect.
    /// </summary>
    [DataField(readOnly: true)]
    [AutoNetworkedField]
    public HashSet<ProtoId<AccessGroupPrototype>> Groups = new();
}

/// <summary>
/// Event raised on an entity to find additional entities which provide access.
/// </summary>
[ByRefEvent]
public struct GetAdditionalAccessEvent
{
    public HashSet<EntityUid> Entities = new();

    public GetAdditionalAccessEvent()
    {
    }
}

[ByRefEvent]
public record struct GetAccessTagsEvent(HashSet<ProtoId<AccessLevelPrototype>> Tags, IPrototypeManager PrototypeManager)
{
    public void AddGroup(ProtoId<AccessGroupPrototype> group)
    {
        if (!PrototypeManager.TryIndex<AccessGroupPrototype>(group, out var groupPrototype))
            return;

        Tags.UnionWith(groupPrototype.Tags);
    }
}
