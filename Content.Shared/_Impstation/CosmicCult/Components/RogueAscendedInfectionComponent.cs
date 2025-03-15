using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.CosmicCult.Components;

/// <summary>
/// Component for revealing cosmic cultists to the crew.
/// </summary>
[NetworkedComponent, RegisterComponent]
public sealed partial class RogueAscendedInfectionComponent : Component
{
    public ResPath RsiPath = new("/Textures/_Impstation/CosmicCult/Effects/ascendantinfection.rsi");
    public readonly string States = "vfx";
    [DataField] public bool HadMoods;
}

[Serializable, NetSerializable]
public enum AscendedInfectionKey
{
    Key
}
