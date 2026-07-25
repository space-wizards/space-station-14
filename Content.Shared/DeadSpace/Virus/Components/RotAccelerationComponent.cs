// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Virus.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RotAccelerationComponent : Component
{
    /// <summary>
    /// Множитель скорости разложения трупа.
    /// </summary>
    [DataField]
    public float DecayMultiplier = 2.5f;
}