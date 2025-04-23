using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.MobStateClothingVisuals;

[RegisterComponent, NetworkedComponent]
public sealed partial class MobStateClothingVisualsComponent : Component
{
    [DataField]
    public string IncapacitatedPrefix = "incapacitated";

    public string? ClothingPrefix = null;
}

public sealed class ClothingMobStateChangedEvent : EntityEventArgs
{
    public ClothingMobStateChangedEvent()
    {

    }
}
