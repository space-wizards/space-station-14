// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.TheCircle.CursedVant;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CursedVantComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SpeedModifier = 0.5f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class CircleDeaconComponent : Component;
