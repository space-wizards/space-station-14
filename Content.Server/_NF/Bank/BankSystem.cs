using System.Threading;
using Content.Server.GameTicking;
using Content.Shared.Bank.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Content.Shared.Database;
using Content.Server.Players;
using Content.Shared.Preferences;
using Content.Server.Database;
using Content.Server.Preferences.Managers;
using Robust.Shared.Log;
using Content.Shared.Audio;

namespace Content.Server.Bank;

public sealed class BankSystem : EntitySystem
{
    [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;

    private ISawmill _log = default!;

    public override void Initialize()
    {
        base.Initialize();
        _log = Logger.GetSawmill("bank");
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
        SubscribeLocalEvent<BankAccountComponent, ComponentGetState>(OnBankAccountChanged);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerLobbyJoin);
    }
    // we may have to change this later depending on mind rework. then again, maybe the bank account should stay attached to the mob and not follow a mind around.
    private void OnPlayerSpawn (PlayerSpawnCompleteEvent args)
    {
        var mobUid = args.Mob;
        var bank = EnsureComp<BankAccountComponent>(mobUid);
        bank.Balance = args.Profile.BankBalance;
        Dirty(bank);
    }

    private void OnBankAccountChanged(EntityUid mobUid, BankAccountComponent bank, ref ComponentGetState args)
    {
        args.State = new BankAccountComponentState
        {
            Balance = bank.Balance,
        };
        var user = args.Player?.UserId;
        if (user == null || args.Player?.AttachedEntity != mobUid)
        {
            _log.Warning($"Could not save bank account");
            return;
        }

        var prefs = _prefsManager.GetPreferences((NetUserId) user);
        var character = prefs.SelectedCharacter;
        var index = prefs.IndexOfCharacter(character);

        if (character is not HumanoidCharacterProfile profile)
            return;
        var newProfile = new HumanoidCharacterProfile(
            profile.Name,
            profile.FlavorText,
            profile.Species,
            profile.Age,
            profile.Sex,
            profile.Gender,
            bank.Balance,
            profile.Appearance,
            profile.Clothing,
            profile.Backpack,
            profile.JobPriorities,
            profile.PreferenceUnavailable,
            profile.AntagPreferences,
            profile.TraitPreferences);

        _dbManager.SaveCharacterSlotAsync((NetUserId) user, newProfile, index);
        _log.Info($"Character {profile.Name} saved");
    }

    private void OnPlayerLobbyJoin (PlayerJoinedLobbyEvent args)
    {
        var cts = new CancellationToken();
        _prefsManager.LoadData(args.PlayerSession, cts);
    }
    public bool TryBankWithdraw(EntityUid mobUid, int amount)
    {
        if (amount <= 0)
        {
            _log.Info($"{amount} is invalid");
            return false;
        }
        if (!TryComp<BankAccountComponent>(mobUid, out var bank))
        {
            _log.Info($"{mobUid} has no bank account");
            return false;
        }
        if (bank.Balance < amount)
        {
            _log.Info($"{mobUid} has insufficient funds");
            return false;
        }
        bank.Balance -= amount;
        _log.Info($"{mobUid} withdrew {amount}");
        Dirty(bank);
        return true;
    }

    public bool TryBankDeposit(EntityUid mobUid, int amount)
    {
        if (amount <= 0)
        {
            _log.Info($"{amount} is invalid");
            return false;
        }
        if (!TryComp<BankAccountComponent>(mobUid, out var bank))
        {
            _log.Info($"{mobUid} has no bank account");
            return false;
        }

        bank.Balance += amount;
        _log.Info($"{mobUid} deposited {amount}");
        Dirty(bank);
        return true;
    }
}
