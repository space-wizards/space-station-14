using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Equipment.Components;

/// <summary>
///     Suppress artifact activation, when entity is placed inside this container.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SuppressArtifactContainerSystem))]
public sealed partial class SuppressArtifactContainerComponent : Component;
