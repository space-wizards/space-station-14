namespace Content.Client.Sandbox.Components;

/// <summary>
/// A marker component for entities that can be copied in sandbox mode.
/// </summary>
/// <remarks>
/// This is useful for entities with HideSpawnMenu set to true that should still be copied.
/// (e.g. alternative pipe layouts). Will not work on abstract prototypes.
/// </remarks>
[RegisterComponent]
public sealed partial class SandboxCopyableComponent : Component;
