using Robust.Shared.Prototypes;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// Entities with this component will be automatically linked to devices that have the specified prototypes.
/// </summary>
[RegisterComponent]
public sealed partial class AlarmAutoLinkComponent : Component
{
    /// <summary>
    /// Prototypes that will automatically link to the entity if they are in the same room.
    /// </summary>
    [DataField]
    public List<ProtoId<EntityPrototype>> AutoLinkPrototypes = new ();
}
