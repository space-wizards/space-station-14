using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Dataset;
using Content.Shared.Tips;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Tips;

/// <summary>
///     Handles periodically displaying gameplay tips to all players ingame.
/// </summary>
public sealed class TipsSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IConsoleHost _conHost = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private bool _tipsEnabled;
    private float _tipTimeOutOfRound;
    private float _tipTimeInRound;
    private string _tipsDataset = "";
    private float _tipTippyChance;

    /// <summary>
    /// Always adds this time to a speech message. This is so really short message stay around for a bit.
    /// </summary>
    private const float SpeechBuffer = 3f;

    /// <summary>
    /// Expected reading speed.
    /// </summary>
    private const float Wpm = 180f;

    [ViewVariables(VVAccess.ReadWrite)]
    private TimeSpan _nextTipTime = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameRunLevelChanged);
        Subs.CVar(_cfg, CCVars.TipFrequencyOutOfRound, SetOutOfRound, true);
        Subs.CVar(_cfg, CCVars.TipFrequencyInRound, SetInRound, true);
        Subs.CVar(_cfg, CCVars.TipsEnabled, SetEnabled, true);
        Subs.CVar(_cfg, CCVars.TipsDataset, SetDataset, true);
        Subs.CVar(_cfg, CCVars.TipsTippyChance, SetTippyChance, true);

        RecalculateNextTipTime();
        _conHost.RegisterCommand("tippy", Loc.GetString("cmd-tippy-desc"), Loc.GetString("cmd-tippy-help"), SendTippy, SendTippyHelper);
        _conHost.RegisterCommand("tip", Loc.GetString("cmd-tip-desc"), "tip", SendTip);
    }

    private CompletionResult SendTippyHelper(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-tippy-auto-1")),
            2 => CompletionResult.FromHint(Loc.GetString("cmd-tippy-auto-2")),
            3 => CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIdsLimited<EntityPrototype>(args[2], _prototype),
                Loc.GetString("cmd-tippy-auto-3")),
            4 => CompletionResult.FromHint(Loc.GetString("cmd-tippy-auto-4")),
            5 => CompletionResult.FromHint(Loc.GetString("cmd-tippy-auto-5")),
            6 => CompletionResult.FromHint(Loc.GetString("cmd-tippy-auto-6")),
            _ => CompletionResult.Empty
        };
    }

    private void SendTip(IConsoleShell shell, string argstr, string[] args)
    {
        AnnounceRandomTip();
        RecalculateNextTipTime();
    }

    private void SendTippy(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteLine(Loc.GetString("cmd-tippy-help"));
            return;
        }

        ActorComponent? actor = null;
        if (args[0] != "all")
        {
            ICommonSession? session;
            if (args.Length > 0)
            {
                // Get player entity
                if (!_playerManager.TryGetSessionByUsername(args[0], out session))
                {
                    shell.WriteLine(Loc.GetString("cmd-tippy-error-no-user"));
                    return;
                }
            }
            else
            {
                session = shell.Player;
            }

            if (session?.AttachedEntity is not { } user)
            {
                shell.WriteLine(Loc.GetString("cmd-tippy-error-no-user"));
                return;
            }

            if (!TryComp(user, out actor))
            {
                shell.WriteError(Loc.GetString("cmd-tippy-error-no-user"));
                return;
            }
        }

        var ev = new TippyEvent(args[1]);

        if (args.Length > 2)
        {
            ev.Proto = args[2];
            if (!_prototype.HasIndex<EntityPrototype>(args[2]))
            {
                shell.WriteError(Loc.GetString("cmd-tippy-error-no-prototype", ("proto", args[2])));
                return;
            }
        }

        if (args.Length > 3)
            ev.SpeakTime = float.Parse(args[3]);
        else
            ev.SpeakTime = GetSpeechTime(ev.Msg);

        if (args.Length > 4)
            ev.SlideTime = float.Parse(args[4]);

        if (args.Length > 5)
            ev.WaddleInterval = float.Parse(args[5]);

        if (actor != null)
            RaiseNetworkEvent(ev, actor.PlayerSession);
        else
            RaiseNetworkEvent(ev);
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_tipsEnabled)
            return;

        if (_nextTipTime != TimeSpan.Zero && _timing.CurTime > _nextTipTime)
        {
            AnnounceRandomTip();
            RecalculateNextTipTime();
        }
    }

    private void SetOutOfRound(float value)
    {
        _tipTimeOutOfRound = value;
    }

    private void SetInRound(float value)
    {
        _tipTimeInRound = value;
    }

    private void SetEnabled(bool value)
    {
        _tipsEnabled = value;

        if (_nextTipTime != TimeSpan.Zero)
            RecalculateNextTipTime();
    }

    private void SetDataset(string value)
    {
        _tipsDataset = value;
    }

    private void SetTippyChance(float value)
    {
        _tipTippyChance = value;
    }

    public static float GetSpeechTime(string text)
    {
        var wordCount = (float)text.Split().Length;
        return SpeechBuffer + wordCount * (60f / Wpm);
    }

    private void AnnounceRandomTip()
    {
        if (!_prototype.TryIndex<LocalizedDatasetPrototype>(_tipsDataset, out var tips))
            return;

        var tip = _random.Pick(tips.Values);
        var msg = Loc.GetString("tips-system-chat-message-wrap", ("tip", Loc.GetString(tip)));

        if (_random.Prob(_tipTippyChance))
        {
            var ev = new TippyEvent(msg);
            ev.SpeakTime = GetSpeechTime(msg);
            RaiseNetworkEvent(ev);
        } else
        {
            _chat.ChatMessageToManyFiltered(Filter.Broadcast(), ChatChannel.OOC, tip, msg,
            EntityUid.Invalid, false, false, Color.MediumPurple);
        }
    }

    private void RecalculateNextTipTime()
    {
        if (_ticker.RunLevel == GameRunLevel.InRound)
        {
            _nextTipTime = _timing.CurTime + TimeSpan.FromSeconds(_tipTimeInRound);
        }
        else
        {
            _nextTipTime = _timing.CurTime + TimeSpan.FromSeconds(_tipTimeOutOfRound);
        }
    }

    private void OnGameRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        // reset for lobby -> inround
        // reset for inround -> post but not post -> lobby
        if (ev.New == GameRunLevel.InRound || ev.Old == GameRunLevel.InRound)
        {
            RecalculateNextTipTime();
        }
    }
}
