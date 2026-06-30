using Robust.Shared.GameStates;

namespace Content.Shared.Audio.AmbientMusic;

/// <summary>
/// Marks an entity as contributing to Engineering ambient music
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AmbientMusicMarkerEngineeringComponent : Component
{

}
