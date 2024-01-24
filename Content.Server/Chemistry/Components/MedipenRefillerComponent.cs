
using Content.Shared.Chemistry;
using Robust.Shared.Audio;

namespace Content.Server.Chemistry.Components;

[RegisterComponent]
public sealed partial class MedipenRefillerComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("recipes")]
    public List<MedipenRecipePrototype> MedipenRecipes = new();

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public float CompletionTime = 20;

    [ViewVariables(VVAccess.ReadOnly)]
    public float RemainingTime = 0;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsActivated = false;
    public string Result = "";

    public SoundPathSpecifier MachineNoise = new("/Audio/Machines/medipen_refiller_activated.ogg");
    public List<string> MedipenList = new List<string>
    {
        "EmergencyMedipen",
        "AntiPoisonMedipen",
        "BruteAutoInjector",
        "BurnAutoInjector",
        "RadAutoInjector",
        "CombatMedipen"
    };
}
