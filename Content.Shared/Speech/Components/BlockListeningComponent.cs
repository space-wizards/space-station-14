using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Causes all ListenAttemptEvents to fail on the entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BlockListeningComponent : Component;
