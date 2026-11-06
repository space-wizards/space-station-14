using Robust.Shared.GameStates;

namespace Content.Shared.Audio.AmbientMusic;

/// <summary>
/// Marks an entity as contributing to Arrivals ambience
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AmbientMusicMarkerArrivalsComponent : Component;
