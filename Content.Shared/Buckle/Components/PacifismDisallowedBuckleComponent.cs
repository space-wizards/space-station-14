using Robust.Shared.GameStates;
namespace Content.Shared.Buckle.Components;
public sealed partial class BuckleComponent : Component;
/// <summary>
/// Makes so a entity with <see cref="StrapComponent"/> don't allow pacifists to buckle other people to it
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PacifismDisallowedBuckleComponent : Component;
