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
using Content.Shared.Weapons.Ranged.Systems;
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
using Robust.Shared.Prototypes;
using Content.Shared.Starlight.GhostTheme;
using Content.Shared.Starlight;
using Robust.Shared.Network;
using Robust.Shared.GameObjects;
using Content.Server.RoundEnd;
using Content.Server.GameTicking;

namespace Content.Server.Ghost.Roles;

[UsedImplicitly]
public sealed class GhostThemeSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISharedPlayersRoleManager _playerRoles = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostComponent, PlayerAttachedEvent>(OnPlayerAttached);
    }

    private readonly Dictionary<ICommonSession, GhostThemeEui> _openUis = [];
    public override void Shutdown()
    {
        base.Shutdown();
    }

    public void OpenEui(ICommonSession session)
    {
        if (session.AttachedEntity is not { Valid: true } attached ||
            !EntityManager.HasComponent<GhostComponent>(attached))
            return;

        if (_openUis.ContainsKey(session))
            CloseEui(session);
        
        HashSet<string> AvailableThemes = new HashSet<string>();
        
        foreach (var ghostTheme in _prototypeManager.EnumeratePrototypes<GhostThemePrototype>())
        {
            if (_playerRoles.HasAnyPlayerFlags(session, ghostTheme.Flags))
            {
                if (ghostTheme.Ckey != null && session.Name != ghostTheme.Ckey && session.Name != $"localhost@{ghostTheme.Ckey}")
                    return;
                AvailableThemes.Add(ghostTheme.ID);
            }
        }

        var eui = _openUis[session] = new GhostThemeEui(AvailableThemes);

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
    public void ChangeTheme(ICommonSession session, string Theme)
    {
        if (session.AttachedEntity is not { Valid: true } attached ||
            !EntityManager.TryGetComponent<GhostThemeComponent>(attached, out var themes))
            return;
            
        themes.SelectedGhostTheme = Theme;
        
        Dirty(attached, themes);
        
        var playerData = _playerRoles.GetPlayerData(attached);
        if (playerData != null)
        {
            Logger.Warning($"current ghost theme: {playerData.GhostTheme}, new ghost theme {Theme}");
            playerData.GhostTheme = Theme;
        }
        
        _appearance.SetData(attached, GhostThemeVisualLayers.Base, Theme);
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
    
    private void OnPlayerAttached(EntityUid uid, GhostComponent component, PlayerAttachedEvent args)
    {
        var theme = EnsureComp<GhostThemeComponent>(uid);
        var playerData = _playerRoles.GetPlayerData(uid);
        if (playerData != null && playerData.GhostTheme != null)
        {
            Logger.Warning($"current ghost theme: {theme.SelectedGhostTheme}, new ghost theme {playerData.GhostTheme}");
            theme.SelectedGhostTheme = playerData.GhostTheme;
            Dirty(uid, theme);
            
            _appearance.SetData(uid, GhostThemeVisualLayers.Base, playerData.GhostTheme);
        }
    }
}

[AnyCommand]
public sealed class GhostTheme : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _e = default!;

    public string Command => "ghostTheme";
    public string Description => "Opens ghost theme preferences window.";
    public string Help => $"{Command}";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player != null)
            _e.System<GhostThemeSystem>().OpenEui(shell.Player);
        else
            shell.WriteLine("You can only open ghost theme UI on a client.");
    }
}
