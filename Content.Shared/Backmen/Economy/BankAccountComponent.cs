// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Backmen.Economy;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class BankAccountComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public string AccountNumber { get; set; } = "000";

    [ViewVariables(VVAccess.ReadOnly)]
    public string AccountPin { get; set; } = "0000";
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public string? AccountName { get; set; }

    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public FixedPoint2 Balance { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public string CurrencyType { get; set; } = "SpaceCash";
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsInfinite { get; set; }

    public void SetBalance(FixedPoint2 newValue)
    {
        Balance = FixedPoint2.Clamp(newValue, 0, FixedPoint2.MaxValue);
    }
}
