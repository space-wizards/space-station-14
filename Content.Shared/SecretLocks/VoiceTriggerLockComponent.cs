using Robust.Shared.GameStates;

namespace Content.Shared.SecretLocks;

/// <summary>
/// Will lock items when triggered with a voice command.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VoiceTriggerLockComponent : Component;
