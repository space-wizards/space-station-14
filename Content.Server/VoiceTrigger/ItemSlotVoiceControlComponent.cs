namespace Content.Server.VoiceTrigger;

/// <summary>
/// entities with this component, ItemSlots, and TriggerOnVoiceComponent will eject the name of the item slot spoken after the TriggerOnVoiceComponent has been activated
/// </summary>
[RegisterComponent]
public sealed partial class ItemSlotVoiceControlComponent : Component;
