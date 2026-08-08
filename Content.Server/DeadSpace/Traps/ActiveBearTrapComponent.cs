// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

namespace Content.Server.DeadSpace.Traps;

/// <summary>
/// Marks a bear trap that currently needs server-side updates while it is arming
/// or while an ensnared target still holds it in a container.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveBearTrapComponent : Component;
