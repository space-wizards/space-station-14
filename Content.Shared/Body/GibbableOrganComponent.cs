using Content.Shared.Gibbing;
using Robust.Shared.GameStates;

namespace Content.Shared.Body;

/// <summary>
/// Component that causes this entity to become gibs when the body it's in is gibbed
/// </summary>
/// <seealso cref="GibbingSystem" />
[RegisterComponent, NetworkedComponent]
[Access(typeof(GibbableOrganSystem))]
public sealed partial class GibbableOrganComponent : Component;
