using Robust.Shared.GameStates;

namespace Content.Shared.Medical;

/// <summary>
/// Prevents an entity or clothing to be defibrillated by a <see cref="DefibrillatorComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DefibrillatorBlockedComponent : Component
{
    [DataField]
    public LocId Popup = "defibrillator-unrevivable";
}