using Content.Client.Weapons.Ranged.EntitySystems;
using Content.Client.Weapons.Ranged.UI;

namespace Content.Client.Weapons.Ranged.Components;

/// <summary>
/// Exposes magazine ammunition information via item status control.
/// </summary>
/// <remarks>
/// Shows the current rounds out of maximum capacity.
/// </remarks>
/// <seealso cref="MagazineItemStatusSystem"/>
/// <seealso cref="MagazineStatusControl"/>
[RegisterComponent]
public sealed partial class MagazineItemStatusComponent : Component;
