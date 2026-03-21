using Robust.Shared.GameStates;

namespace Content.Shared.Carrying.Components;

/// <summary>
/// Active marker component on an entity currently carrying another entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveCarrierComponent : Component;
