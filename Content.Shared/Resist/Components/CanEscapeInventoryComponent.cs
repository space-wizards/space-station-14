using Content.Shared.DoAfter;
using Content.Shared.Resist.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Resist.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(EscapeInventorySystem))]
public sealed partial class CanEscapeInventoryComponent : Component
{
    public bool IsEscaping => DoAfter != null;

    /// <summary>
    /// Base doafter length for uncontested breakouts.
    /// </summary>
    [DataField]
    public float BaseResistTime = 5f;


    [DataField]
    public DoAfterId? DoAfter;
}
