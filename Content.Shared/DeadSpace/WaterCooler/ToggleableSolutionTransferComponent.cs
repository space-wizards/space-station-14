// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.WaterCooler;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class ToggleableSolutionTransferComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Solution = "tank";

    [DataField, AutoNetworkedField]
    public SolutionTransferDirection Direction = SolutionTransferDirection.Output;
}

public enum SolutionTransferDirection
{
    Input,
    Output,
}
