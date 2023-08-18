using Content.Server.Administration.Logs;
using Content.Server.GameTicking.Rules;
using Content.Server.Popups;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.NukeOps;
using Robust.Server.GameObjects;

namespace Content.Server.NukeOps;

public sealed class WarDeclaratorSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly NukeopsRuleSystem _nukeopsRuleSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WarDeclaratorComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<WarDeclaratorComponent, WarDeclaratorActivateMessage>(OnActivated);
    }

    private void OnActivate(EntityUid uid, WarDeclaratorComponent component, ActivateInWorldEvent args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        args.Handled = true;

        var isNukeOps = _nukeopsRuleSystem.TryGetOperativeRuleComponents(args.User, out _);
        if (!isNukeOps)
        {
            var msg = Loc.GetString("war-declarator-not-nukeops");
            _popupSystem.PopupEntity(msg, uid);
            return;
        }

        _userInterfaceSystem.TryOpen(uid, WarDeclaratorUiKey.Key, actor.PlayerSession);
        UpdateUI(uid, args.User);
    }

    private void OnActivated(EntityUid uid, WarDeclaratorComponent component, WarDeclaratorActivateMessage args)
    {
        if (!args.Session.AttachedEntity.HasValue ||
            !_nukeopsRuleSystem.TryGetOperativeRuleComponents(args.Session.AttachedEntity.Value, out var comps))
            return;

        var condition = _nukeopsRuleSystem.GetWarCondition(comps.Value.Item1, comps.Value.Item2);
        if (condition != WarConditionStatus.YES_WAR)
        {
            UpdateUI(uid, args.Session.AttachedEntity.Value);
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

        var message = new string(text);
        if (component.AllowEditMessage && message != string.Empty)
            component.Message = message;

        message = Loc.GetString(message);
        var title = Loc.GetString(component.DeclarementTitle);

        _nukeopsRuleSystem.DeclareWar(args.Session.AttachedEntity.Value, message, title, component.DeclarementSound, component.DeclarementColor);

        if (args.Session.AttachedEntity != null)
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"{ToPrettyString(args.Session.AttachedEntity.Value):player} has declared war with this text: {message}");
    }

    public void RefreshAllUI(EntityUid opsUid)
    {
        var enumerator = EntityQueryEnumerator<WarDeclaratorComponent>();
        while (enumerator.MoveNext(out var uid, out _))
        {
            UpdateUI(uid, opsUid);
        }
    }

    private void UpdateUI(EntityUid declaratorUid, EntityUid opsUid)
    {
        if (!_nukeopsRuleSystem.TryGetOperativeRuleComponents(opsUid, out var comps))
            return;
        var condition = _nukeopsRuleSystem.GetWarCondition(comps.Value.Item1, comps.Value.Item2);

        TimeSpan startTime;
        TimeSpan delayTime;
        switch(condition)
        {
            case WarConditionStatus.YES_WAR:
                startTime = comps.Value.Item2.ActivatedAt;
                delayTime = comps.Value.Item1.WarDeclarationDelay;
                break;
            case WarConditionStatus.WAR_DELAY:
                startTime = comps.Value.Item1.WarDeclaredTime!.Value;
                delayTime = comps.Value.Item1.WarNukieArriveDelay;
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
                comps.Value.Item1.WarDeclarationMinOps,
                delayTime,
                startTime));
    }
}
