using Robust.Shared.GameStates;

namespace Content.Shared.ParcelWrap.Components;

/// <summary>
/// This component is a list of overrides that are applied to the wrapping paper.
/// The targeted entity to override should be given a tag that is checked to be overwritten in the whitelist.
/// Parcel wraps can have multiple different overrides, each with their own tag whitelists, wrap times and results.
/// </summary>
/// <remarks>
/// Use this for special, non-item parcel types, for example Urist-shaped parcels.
/// </remarks>
/// <code>
/// overrides:
/// - protoToUse: WrappedPresentHumanoid
///   wrapDelay: 5.0
///   whitelist:
///     tags:
///     - HumanoidParcelShape
/// </code>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ParcelWrapOverrideComponent : Component
{
    /// <summary>
    /// This is a list of override conditions for this parcel wrap.
    /// If an entity matches multiple overrides, the first one listed will take priority.
    /// Use this for special, non-item parcel types, for example Urist-shaped parcels.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public List<ParcelWrapOverrideData> Overrides;
}
