using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server._Den.Components;


[RegisterComponent, NetworkedComponent]
public sealed partial class VampireDrinkBloodComponent : Component
{
    [DataField]
    public TimeSpan DrinkBloodDuration = TimeSpan.FromSeconds(2);

    [DataField]
    public EntProtoId ActionProto = "ActionVampireDrinkBlood";

    [DataField]
    public EntityUid? Action;

    [DataField]
    public List<ProtoId<ReagentPrototype>> BloodTarget = new()
    {
        "Blood",
        "CopperBlood",
        "InsectBlood"
    };

    [DataField]
        public float BloodDrainAmount = 20f;
}
