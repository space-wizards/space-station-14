using Content.Shared.Damage.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

/// <summary>
/// With this component, <see cref="StaminaDamageOnHitComponent"/> will not work unless the item is toggled on.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedStaminaSystem))]
public sealed partial class StaminaDamageOnHitRequiresToggleComponent : Component;
