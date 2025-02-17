// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Backmen.Economy.Eftpos;

[NetworkedComponent]
public abstract partial class SharedEftposComponent : Component
{
    [Serializable, NetSerializable]
    public sealed class EftposBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly FixedPoint2? Value;
        public readonly string? LinkedAccountNumber;
        public readonly string? LinkedAccountName;
        public readonly bool IsLocked;
        public readonly string? CurrencySymbol;
        public EftposBoundUserInterfaceState(FixedPoint2? value, string? linkedAccountNumber, string? linkedAccountName, bool isLocked, string? currencySymbol)
        {
            Value = value;
            LinkedAccountNumber = linkedAccountNumber;
            LinkedAccountName = linkedAccountName;
            IsLocked = isLocked;
            CurrencySymbol = currencySymbol;
        }
    }
}
