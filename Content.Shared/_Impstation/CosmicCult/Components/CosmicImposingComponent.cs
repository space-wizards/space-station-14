using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.CosmicCult.Components;

/// <summary>
/// Component for displaying Vacuous Imposition's visuals on a player.
/// </summary>
[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class CosmicImposingComponent : Component
{
    public ResPath RsiPath = new("/Textures/_Impstation/CosmicCult/Effects/ability_imposition_overlay.rsi");
    public readonly string States = "vfx";
}

[Serializable, NetSerializable]
public enum CosmicImposingKey
{
    Key
}
