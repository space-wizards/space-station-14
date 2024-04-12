namespace Content.Server.Audio;

/// <summary>
/// Toggles <see cref="AmbientSoundComponent"/> off when this entity's MobState is Dead.
/// </summary>
[RegisterComponent]
public sealed partial class SoundWhileAliveComponent : Component;
