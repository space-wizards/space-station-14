// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.Backmen.Economy.WageConsole;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedWageConsoleSystem))]
public sealed partial class WageConsoleComponent : Component
{

}
