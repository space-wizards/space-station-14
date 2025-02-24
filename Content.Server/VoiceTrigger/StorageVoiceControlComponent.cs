namespace Content.Server.VoiceTrigger;

/// <summary>
/// Entities with this component, Containers, and TriggerOnVoiceComponent will insert any item or extract the spoken item after the TriggerOnVoiceComponent has been activated
/// </summary>
[RegisterComponent]
public sealed partial class StorageVoiceControlComponent : Component;
