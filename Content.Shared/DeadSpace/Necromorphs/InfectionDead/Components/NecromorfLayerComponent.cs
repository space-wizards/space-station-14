// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class NecromorfLayerComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtilUpdate = TimeSpan.Zero;

    [DataField]
    public string Sprite = string.Empty;

    [DataField]
    public string State = string.Empty;

    [DataField]
    public bool IsAnimal = false;

    public NecromorfLayerComponent(string sprite, string state, bool isAnimal)
    {
        Sprite = sprite;
        State = state;
        IsAnimal = isAnimal;
    }
}
