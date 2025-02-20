// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT
// Official port from the BACKMEN project. Make sure to review the original repository to avoid license violations.

using Robust.Shared.Serialization;

namespace Content.Shared.Backmen.FootPrint;

[Serializable, NetSerializable]
public sealed class FootPrintState(NetEntity netEntity) : ComponentState
{
    public NetEntity PrintOwner { get; private set; } = netEntity;
}
