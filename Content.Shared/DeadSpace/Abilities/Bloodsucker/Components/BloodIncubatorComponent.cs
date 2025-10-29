// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Abilities.Bloodsucker.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodIncubatorComponent : Component
{

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float StartBloodVolume = 1;

    [DataField]
    public List<string> States { get; set; } = new List<string>();
}

[Serializable, NetSerializable]
public sealed class BloodIncubatorComponentState : ComponentState
{
    public int State;

    public BloodIncubatorComponentState(int state)
    {
        State = state;
    }
}