// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Backmen.Economy.ATM;

[NetworkedComponent, RegisterComponent]
public sealed partial class AtmComponent : Component
{
    public static string IdCardSlotId = "IdCardSlot";

    [DataField("idCardSlot")]
    public ItemSlot IdCardSlot = new();

    [DataField("offState")]
    public string? OffState;
    [DataField("normalState")]
    public string? NormalState;

    [ViewVariables(VVAccess.ReadOnly), DataField("currencyWhitelist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<CurrencyPrototype>))]
    public HashSet<string> CurrencyWhitelist = new();

    [DataField("soundInsertCurrency")]
    // Taken from: https://github.com/Baystation12/Baystation12 at commit 662c08272acd7be79531550919f56f846726eabb
    public SoundSpecifier SoundInsertCurrency = new SoundPathSpecifier("/Audio/_Backmen/Machines/polaroid2.ogg");
    [DataField("soundWithdrawCurrency")]
    // Taken from: https://github.com/Baystation12/Baystation12 at commit 662c08272acd7be79531550919f56f846726eabb
    public SoundSpecifier SoundWithdrawCurrency = new SoundPathSpecifier("/Audio/_Backmen/Machines/polaroid1.ogg");
    [DataField("soundApply")]
    // Taken from: https://github.com/Baystation12/Baystation12 at commit 662c08272acd7be79531550919f56f846726eabb
    public SoundSpecifier SoundApply = new SoundPathSpecifier("/Audio/_Backmen/Machines/chime.ogg");
    [DataField("soundDeny")]
    // Taken from: https://github.com/Baystation12/Baystation12 at commit 662c08272acd7be79531550919f56f846726eabb
    public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/_Backmen/Machines/buzz-sigh.ogg");
}

[Serializable, NetSerializable]
public sealed class AtmBoundUserInterfaceBalanceState : BoundUserInterfaceState
{
    public readonly FixedPoint2? BankAccountBalance;
    public readonly string? CurrencySymbol;
    public AtmBoundUserInterfaceBalanceState(
        FixedPoint2? bankAccountBalance,
        string? currencySymbol)
    {
        BankAccountBalance = bankAccountBalance;
        CurrencySymbol = currencySymbol;
    }
}

[Serializable, NetSerializable]
public sealed class AtmBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly bool IsCardPresent;
    public readonly string? IdCardFullName;
    public readonly string? IdCardEntityName;
    public readonly string? IdCardStoredBankAccountNumber;
    public readonly bool HaveAccessToBankAccount;
    public readonly FixedPoint2? BankAccountBalance;
    public readonly string? CurrencySymbol;
    public AtmBoundUserInterfaceState(
        bool isCardPresent,
        string? idCardFullName,
        string? idCardEntityName,
        string? idCardStoredBankAccountNumber,
        bool haveAccessToBankAccount,
        FixedPoint2? bankAccountBalance,
        string? currencySymbol)
    {
        IsCardPresent = isCardPresent;
        IdCardFullName = idCardFullName;
        IdCardEntityName = idCardEntityName;
        IdCardStoredBankAccountNumber = idCardStoredBankAccountNumber;
        HaveAccessToBankAccount = haveAccessToBankAccount;
        BankAccountBalance = bankAccountBalance;
        CurrencySymbol = currencySymbol;
    }
}

[Serializable, NetSerializable]
public enum ATMVisuals
{
    VisualState
}

[Serializable, NetSerializable]
public enum ATMVisualState
{
    Normal,
    Off
}
