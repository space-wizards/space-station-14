using Content.Shared.Construction.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._EE.ShortConstruction;

//This was originally a PR for Einstein's Engines, submitted by Github user VMSolidus.
//https://github.com/Simple-Station/Einstein-Engines/pull/861
//It has been modified to work within the Imp Station 14 server fork by Honeyed_Lemons.

[RegisterComponent, NetworkedComponent]
public sealed partial class ShortConstructionComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<ConstructionPrototype>> Prototypes = new();
}

[NetSerializable, Serializable]
public enum ShortConstructionUiKey : byte
{
    Key,
}
