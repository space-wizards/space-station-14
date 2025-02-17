// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Backmen.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class BankUiState : BoundUserInterfaceState
{
    public FixedPoint2? LinkedAccountBalance;
    public BankUiState(
        FixedPoint2 ? linkedAccountBalance = null
        )
    {
        LinkedAccountBalance = linkedAccountBalance;
    }
}
