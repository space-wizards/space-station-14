using Content.Shared.Cuffs.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// An entity with this component will only apply the accent if the entity is cuffed.
/// Requires <see cref="HandcuffComponent"/> and an accent component.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AccentRequireCuffedComponent : Component;
