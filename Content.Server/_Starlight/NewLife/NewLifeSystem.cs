using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.EUI;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Shared.Ghost.Roles.Raffles;
using Content.Server.Ghost.Roles.UI;
using Content.Server.Mind.Commands;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Follower;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Ghost.Roles;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Players;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Server.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Collections;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Starlight.NewLife;
using Robust.Shared.Network;
using Content.Server.RoundEnd;
using Content.Server.GameTicking;
using Content.Shared.Starlight.CCVar;

namespace Content.Server.Ghost.Roles;

[UsedImplicitly]
public sealed class NewLifeSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;

    private readonly Dictionary<ICommonSession, NewLifeEui> _openUis = [];
    private readonly Dictionary<NetUserId, HashSet<int>> _roundCharactersUsed = [];
    private readonly Dictionary<NetUserId, int> _newLifesLeft = [];
    private int MaxNewLifes = 5;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundEndSystemChangedEvent>(ClearRoundCharacterUsed);
        //cvar for max new lifes
        _configuration.OnValueChanged(StarlightCCVars.MaxNewLifes, UpdateMaxNewLifes, true);
    }

    private void UpdateMaxNewLifes(int value)
    {
        MaxNewLifes = value;
        //update all open uis
        UpdateAllEui();
    }

    public override void Shutdown()
    {
        base.Shutdown();
    }
    public void ClearRoundCharacterUsed(RoundEndSystemChangedEvent _)
    {
        if (_gameTicker.RunLevel == GameRunLevel.PreRoundLobby)
        {
            _roundCharactersUsed.Clear();
            _newLifesLeft.Clear();
        }
    }

    public void OpenEui(ICommonSession session)
    {
        if (session.AttachedEntity is not { Valid: true } attached ||
            !EntityManager.HasComponent<GhostComponent>(attached))
            return;

        if (_openUis.ContainsKey(session))
            CloseEui(session);

        var usedSlots = _roundCharactersUsed.TryGetValue(session.UserId, out var slots) ? slots : [];
        var remainingLives = _newLifesLeft.TryGetValue(session.UserId, out var remaining) ? remaining : MaxNewLifes;
        var eui = _openUis[session] = new NewLifeEui(usedSlots, remainingLives, MaxNewLifes);

        _euiManager.OpenEui(eui, session);
        eui.StateDirty();
    }
    public void CloseEui(ICommonSession session)
    {
        if (!_openUis.ContainsKey(session))
            return;

        _openUis.Remove(session, out var eui);

        eui?.Close();
    }
    public void UpdateAllEui()
    {
        foreach (var eui in _openUis.Values)
        {
            eui.StateDirty();
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
    }

    internal void SaveCharacterToUsed(NetUserId userId, int slot)
    {
        if (_roundCharactersUsed.TryGetValue(userId, out var characters))
            characters.Add(slot);
        else
            _roundCharactersUsed.Add(userId, [slot]);

        //subtract from remaining slots
        if (_newLifesLeft.TryGetValue(userId, out var remaining))
        {
            remaining--;
            _newLifesLeft[userId] = remaining;
        }
        else
        {
            _newLifesLeft.Add(userId, MaxNewLifes - 1);
        }
    }
    internal bool SlotIsAvailable(NetUserId userId, int slot)
        => (!_roundCharactersUsed.TryGetValue(userId, out var characters))
        || !characters.Contains(slot);
}

[AnyCommand]
public sealed class NewLife : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _e = default!;

    public string Command => "newlife";
    public string Description => "Opens the new life request window.";
    public string Help => $"{Command}";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player != null)
            _e.System<NewLifeSystem>().OpenEui(shell.Player);
        else
            shell.WriteLine("You can only open the new life UI on a client.");
    }
}
