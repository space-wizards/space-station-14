
using Content.Shared.Chemistry;

namespace Content.Server.Chemistry.Components;

[RegisterComponent]
public sealed partial class MedipenRefillerComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("recipes")]
    public List<MedipenRecipePrototype> MedipenRecipes = new();
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
