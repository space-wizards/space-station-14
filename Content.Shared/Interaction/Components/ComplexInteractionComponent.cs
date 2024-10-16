using Robust.Shared.GameStates;

namespace Content.Shared.Interaction.Components;

/// <summary>
/// This is used for identifying entities as being able to use complex interactions with the environment.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedInteractionSystem))]
public sealed partial class ComplexInteractionComponent : Component;
