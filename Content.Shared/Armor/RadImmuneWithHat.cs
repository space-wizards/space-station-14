using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Armor;

/// <summary>
/// Used for clothing that reduces radiation damage when worn with specific hat.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(RadImmuneClothingSystem))]
public sealed partial class ArmorHelmetComponent : Component
{

}
