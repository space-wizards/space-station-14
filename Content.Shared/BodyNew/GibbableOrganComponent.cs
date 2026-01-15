using Robust.Shared.GameStates;

namespace Content.Shared.BodyNew;

[RegisterComponent, NetworkedComponent]
[Access(typeof(GibbableOrganSystem))]
public sealed partial class GibbableOrganComponent : Component;
