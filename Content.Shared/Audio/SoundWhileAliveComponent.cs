using Content.Shared.Sound.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Audio;

/// <summary>
/// Toggles <see cref="AmbientSoundComponent"/> and <see cref="SpamEmitSoundComponent"/> off when this entity's MobState isn't Alive.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SoundWhileAliveComponent : Component;
