using Content.Server.Administration.Managers;
using Content.Server.Hands.Systems;
using Content.Server.Stack;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Starlight.Economy.Atm;
public sealed partial class ATMSystem : SharedATMSystem
{
    [Dependency] private readonly IPlayerRolesManager _playerRolesManager = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    private static readonly EntProtoId<StackComponent> _cash = "NTCredit";
    public override void Initialize()
    {
        SubscribeLocalEvent<ATMComponent, BeforeActivatableUIOpenEvent>(OnBeforeActivatableUIOpen);
        SubscribeLocalEvent<NTCashComponent, AfterInteractEvent>(OnAfterInteract);
        Subs.BuiEvents<ATMComponent>(ATMUIKey.Key, subs => subs.Event<ATMWithdrawBuiMsg>(OnWithdraw));
        base.Initialize();
    }

    private void OnWithdraw(EntityUid uid, ATMComponent component, ATMWithdrawBuiMsg args)
    {
        if (_playerRolesManager.GetPlayerData(args.Actor) is not PlayerData playerData
            || playerData.Balance < args.Amount
            || args.Amount <= 0) return;

        playerData.Balance -= args.Amount;
        var cash = SpawnAtPosition(_cash, Transform(uid).Coordinates);
        var stack = EnsureComp<StackComponent>(cash);
        _stack.SetCount(cash, args.Amount, stack);
        _hands.TryPickup(args.Actor, cash);
        _uiSystem.SetUiState(uid, ATMUIKey.Key, new ATMBuiState() { Balance = playerData.Balance });
        _audioSystem.PlayPvs(component.WithdrawSound, uid);
    }

    private void OnAfterInteract(Entity<NTCashComponent> ent, ref AfterInteractEvent args)
    {
        if (TryComp<StackComponent>(ent.Owner, out var stack)
            && args.Target.HasValue
            && TryComp<ATMComponent>(args.Target, out var atm)
            && _playerRolesManager.GetPlayerData(args.User) is PlayerData playerData)
        {
            playerData.Balance += (int)Math.Floor(stack.Count * 0.9);
            QueueDel(ent);
            _uiSystem.SetUiState(args.Target.Value, ATMUIKey.Key, new ATMBuiState() { Balance = playerData.Balance });
            _audioSystem.PlayPvs(atm.DepositSound, args.Target.Value);
        }
    }

    private void OnBeforeActivatableUIOpen(Entity<ATMComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        var playerData = _playerRolesManager.GetPlayerData(args.User);

        _uiSystem.SetUiState(ent.Owner, ATMUIKey.Key, new ATMBuiState() { Balance = playerData?.Balance ?? 0 });
    }
}