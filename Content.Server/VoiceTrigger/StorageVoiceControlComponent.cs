using Content.Shared.Inventory;

namespace Content.Server.VoiceTrigger;

/// <summary>
/// Entities with this component, Containers, and TriggerOnVoiceComponent will insert any item or extract the spoken item after the TriggerOnVoiceComponent has been activated
/// </summary>
[RegisterComponent]
public sealed partial class StorageVoiceControlComponent : Component
{
    /// <summary>
    /// Used to determine which slots the component can be used in.
    /// <remarks>
    /// If not set, the component can be used anywhere, even while inside other containers.
    /// </remarks>
    /// </summary>
    [DataField]
    public SlotFlags? AllowedSlots;
}
