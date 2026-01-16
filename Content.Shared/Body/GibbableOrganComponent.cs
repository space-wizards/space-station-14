using Robust.Shared.GameStates;

namespace Content.Shared.Body;

[RegisterComponent, NetworkedComponent]
[Access(typeof(GibbableOrganSystem))]
public sealed partial class GibbableOrganComponent : Component;
