using Content.Shared.NPC.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

// This component is for mobs with pack behavior traits

namespace Content.Shared._Starlight.Behaviors.Pack;
[RegisterComponent, NetworkedComponent]

public sealed partial class QuoremCheckComponent : Component 
{
    [DataField] public float DetectionRadius = 5.0f;
    
    [DataField] public int QuoremThreshold = 3;

    ///[DataField (readOnly:true)] public int CurrentPackSize;

    [DataField(required: true)] public string PackTag;

    [DataField(readOnly: true)] public bool IsHostile;

    ///[DataField] public bool CanRepacify;
    [DataField] public int PackId;
    
    [DataField] public ProtoId<NpcFactionPrototype> QuoremFaction = "Xeno";
    
    [DataField] public ProtoId<NpcFactionPrototype> DefaultFaction = "Passive";

    [DataField] public ProtoId<EntityPrototype>? QuoremEffect;
    
    [DataField] public SoundSpecifier? QuoremSound;


}

