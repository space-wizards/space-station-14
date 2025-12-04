using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ParcelWrap.Components;
/// <summary>
/// This is a single entry in <see cref="ParcelWrapOverrideComponent"/>. See for usage example.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class ParcelWrapOverrideData
{
    /// <summary>
    /// The <see cref="EntityPrototype"/> of the parcel created by wrapping this entity.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId ProtoToUse;

    /// <summary>
    /// The <see cref="EntityWhitelist"/> that an entity is required to pass to be overwritten by ProtoToUse.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// How long it takes to wrap this entity.
    /// </summary>
    [DataField]
    public TimeSpan? WrapDelay;
}
