using Content.Shared.Access;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Content.Shared.Radio;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Cargo.Components;

/// <summary>
/// Handles sending order requests to cargo. Doesn't handle orders themselves via shuttle or telepads.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedCargoSystem))]
public sealed partial class CargoOrderConsoleComponent : Component
{
    /// <summary>
    /// The account that this console pulls from for ordering.
    /// </summary>
    [DataField]
    public ProtoId<CargoAccountPrototype> Account = "Cargo";

    [DataField]
    public SoundSpecifier ErrorSound = new SoundCollectionSpecifier("CargoError");

    /// <summary>
    /// Sound made when <see cref="TransferUnbounded"/> is toggled.
    /// </summary>
    [DataField]
    public SoundSpecifier ToggleLimitSound = new SoundCollectionSpecifier("CargoToggleLimit");

    /// <summary>
    /// If true, account transfers have no limit and a lower cooldown.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TransferUnbounded;

    [ViewVariables]
    public float TransferLimit => TransferUnbounded ? 1 : BaseTransferLimit;

    /// <summary>
    /// The maximum percent of total funds that can be transferred or withdrawn in one action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BaseTransferLimit = 0.20f;

    /// <summary>
    /// The time at which account actions can be performed again.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextAccountActionTime;

    [ViewVariables]
    public TimeSpan AccountActionDelay => TransferUnbounded ? UnboundedAccountActionDelay : BaseAccountActionDelay;

    /// <summary>
    /// The minimum time between account actions when <see cref="TransferUnbounded"/> is false
    /// </summary>
    [DataField]
    public TimeSpan BaseAccountActionDelay = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The minimum time between account actions when <see cref="TransferUnbounded"/> is true
    /// </summary>
    [DataField]
    public TimeSpan UnboundedAccountActionDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The stack representing cash dispensed on withdrawals.
    /// </summary>
    [DataField]
    public ProtoId<StackPrototype> CashType = "Credit";

    /// <summary>
    /// All of the <see cref="CargoProductPrototype.Group"/>s that are supported.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<CargoMarketPrototype>> AllowedGroups = new() { "market" };

    /// <summary>
    /// Access needed to toggle the limit on this console.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<AccessLevelPrototype>> RemoveLimitAccess = new();

    /// <summary>
    /// Radio channel on which order approval announcements are transmitted
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<RadioChannelPrototype> AnnouncementChannel = "Supply";

    /// <summary>
    /// Secondary radio channel which always receives order announcements.
    /// </summary>
    public static readonly ProtoId<RadioChannelPrototype> BaseAnnouncementChannel = "Supply";

    /// <summary>
    /// If set to true, restricts this console from ordering and has it print slips instead
    /// </summary>
    [DataField]
    public bool SlipPrinter;

    /// <summary>
    /// The time at which the console will be able to print a slip again.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPrintTime = TimeSpan.Zero;

    /// <summary>
    /// The time between prints.
    /// </summary>
    [DataField]
    public TimeSpan PrintDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The sound made when printing occurs
    /// </summary>
    [DataField]
    public SoundSpecifier PrintSound = new SoundCollectionSpecifier("PrinterPrint");

    /// <summary>
    /// The sound made when an order slip is scanned
    /// </summary>
    [DataField]
    public SoundSpecifier ScanSound = new SoundCollectionSpecifier("CargoBeep");

    /// <summary>
    /// The time at which the console will be able to play the deny sound.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextDenySoundTime = TimeSpan.Zero;

    /// <summary>
    /// The time between playing the deny sound.
    /// </summary>
    [DataField]
    public TimeSpan DenySoundDelay = TimeSpan.FromSeconds(2);
}

/// <summary>
/// Withdraw funds from an account
/// </summary>
[Serializable, NetSerializable]
public sealed class CargoConsoleWithdrawFundsMessage : BoundUserInterfaceMessage
{
    public ProtoId<CargoAccountPrototype>? Account;
    public int Amount;

    public CargoConsoleWithdrawFundsMessage(ProtoId<CargoAccountPrototype>? account, int amount)
    {
        Account = account;
        Amount = amount;
    }
}

/// <summary>
/// Toggle the limit on withdrawals and transfers.
/// </summary>
[Serializable, NetSerializable]
public sealed class CargoConsoleToggleLimitMessage : BoundUserInterfaceMessage;
