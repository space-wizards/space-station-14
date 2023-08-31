using Content.Server.Administration.Logs;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Popups;
using Content.Server.UserInterface;
using Content.Shared.Database;
using Content.Shared.NukeOps;
using Robust.Server.GameObjects;

namespace Content.Server.NukeOps;

/// <summary>
///     This handles nukeops special war mode declaration device and directly using nukeops game rule
/// </summary>
public sealed class WarDeclaratorSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly NukeopsRuleSystem _nukeopsRuleSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WarDeclaratorComponent, WarDeclaratorActivateMessage>(OnActivated);
        SubscribeLocalEvent<WarDeclaratorComponent, ActivatableUIOpenAttemptEvent>(OnAttemptOpenUI);
    }

    private void OnAttemptOpenUI(EntityUid uid, WarDeclaratorComponent component, ActivatableUIOpenAttemptEvent args)
    {
        if (!_nukeopsRuleSystem.TryGetRuleFromOperative(args.User, out var comps))
        {
            var msg = Loc.GetString("war-declarator-not-nukeops");
            _popupSystem.PopupEntity(msg, uid);
            args.Cancel();
            return;
        }

        UpdateUI(uid, comps.Value.Item1, comps.Value.Item2);
    }

    private void OnActivated(EntityUid uid, WarDeclaratorComponent component, WarDeclaratorActivateMessage args)
    {
        if (!args.Session.AttachedEntity.HasValue ||
            !_nukeopsRuleSystem.TryGetRuleFromOperative(args.Session.AttachedEntity.Value, out var comps))
            return;

        var condition = _nukeopsRuleSystem.GetWarCondition(comps.Value.Item1, comps.Value.Item2);
        if (condition != WarConditionStatus.YES_WAR)
        {
            UpdateUI(uid, comps.Value.Item1, comps.Value.Item2);
            return;
        }

        var text = (args.Message.Length <= component.MaxMessageLength ? args.Message.Trim() : $"{args.Message.Trim().Substring(0, 256)}...").ToCharArray();

        // No more than 2 newlines, other replaced to spaces
        var newlines = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '\n')
                continue;

            if (newlines >= 2)
                text[i] = ' ';

            newlines++;
        }

        string message = new string(text);
        if (component.AllowEditingMessage && message != string.Empty)
        {
            component.Message = message;
        }
        else
        {
            message = Loc.GetString("war-declarator-default-message");
        }
        var title = Loc.GetString(component.DeclarementTitle);

        _nukeopsRuleSystem.DeclareWar(args.Session.AttachedEntity.Value, message, title, component.DeclarementSound, component.DeclarementColor);

        if (args.Session.AttachedEntity != null)
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"{ToPrettyString(args.Session.AttachedEntity.Value):player} has declared war with this text: {message}");
    }

    public void RefreshAllUI(NukeopsRuleComponent nukeops, GameRuleComponent gameRule)
    {
        var enumerator = EntityQueryEnumerator<WarDeclaratorComponent>();
        while (enumerator.MoveNext(out var uid, out _))
        {
            UpdateUI(uid, nukeops, gameRule);
        }
    }

    private void UpdateUI(EntityUid declaratorUid, NukeopsRuleComponent nukeops, GameRuleComponent gameRule)
    {
        var condition = _nukeopsRuleSystem.GetWarCondition(nukeops, gameRule);

        TimeSpan startTime;
        TimeSpan delayTime;
        switch(condition)
        {
            case WarConditionStatus.YES_WAR:
                startTime = gameRule.ActivatedAt;
                delayTime = nukeops.WarDeclarationDelay;
                break;
            case WarConditionStatus.WAR_DELAY:
                startTime = nukeops.WarDeclaredTime!.Value;
                delayTime = nukeops.WarNukieArriveDelay!.Value;
                break;
            default:
                startTime = TimeSpan.Zero;
                delayTime = TimeSpan.Zero;
                break;
        }

        _userInterfaceSystem.TrySetUiState(
            declaratorUid,
            WarDeclaratorUiKey.Key,
            new WarDeclaratorBoundUserInterfaceState(
                condition,
                nukeops.WarDeclarationMinOps,
                delayTime,
                startTime));
    }
}
