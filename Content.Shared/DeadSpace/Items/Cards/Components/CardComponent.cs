using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Items.Cards.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CardComponent : Component
{
    #region Visualizer
    [DataField("state")]
    public string State = "based";

    [DataField("reserveState")]
    public string ReserveState = "clubs-6";
    #endregion

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsReserve = false;

}
