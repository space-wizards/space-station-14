using Content.Client.Atmos.EntitySystems;
using Content.Client.Atmos.UI;

namespace Content.Client.Atmos.Components;

/// <summary>
/// Exposes gas tank pressure information via item status control.
/// </summary>
/// <remarks>
/// Shows the tank pressure in kPa and Open/Closed state.
/// </remarks>
/// <seealso cref="TankPressureItemStatusSystem"/>
/// <seealso cref="TankPressureStatusControl"/>
[RegisterComponent]
public sealed partial class TankPressureItemStatusComponent : Component;
