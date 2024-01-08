using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.KillTracking;
using Content.Shared.Chat;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// This handles calling out kills from <see cref="KillTrackingSystem"/>
/// </summary>
public sealed class KillCalloutRuleSystem : GameRuleSystem<KillCalloutRuleComponent>
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KillReportedEvent>(OnKillReported);
    }

    private void OnKillReported(ref KillReportedEvent ev)
    {
        var query = EntityQueryEnumerator<KillCalloutRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var kill, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            var callout = GetCallout(kill, ev);
            _chatManager.ChatMessageToAll(ChatChannel.Server, callout, callout, uid, false, true, Color.OrangeRed);
        }
    }

    private string GetCallout(KillCalloutRuleComponent component, KillReportedEvent ev)
    {
        // Do the humiliation callouts if you kill yourself or die from bleeding out or something lame.
        if (ev.Primary is KillEnvironmentSource || ev.Suicide)
        {
            var selfCallout = $"{component.SelfKillCalloutPrefix}{_random.Next(component.SelfKillCalloutAmount)}";
            return Loc.GetString(selfCallout,
                ("victim", GetCalloutName(ev.Entity)));
        }

        var primary = GetCalloutName(ev.Primary);
        var killerString = primary;
        if (ev.Assist != null)
        {
            var secondary = GetCalloutName(ev.Assist);
            killerString = Loc.GetString("death-match-assist",
                ("primary", primary), ("secondary", secondary));
        }

        var callout = $"{component.KillCalloutPrefix}{_random.Next(component.KillCalloutAmount)}";
        return Loc.GetString(callout, ("killer", killerString),
            ("victim", GetCalloutName(ev.Entity)));
    }

    private string GetCalloutName(KillSource source)
    {
        switch (source)
        {
            case KillPlayerSource player:
                if (!_playerManager.TryGetSessionById(player.PlayerId, out var session))
                    break;
                if (session.AttachedEntity == null)
                    break;

                return Loc.GetString("death-match-name-player",
                    ("name", MetaData(session.AttachedEntity.Value).EntityName),
                    ("username", session.Name));

            case KillNpcSource npc:
                if (Deleted(npc.NpcEnt))
                    return string.Empty;
                return Loc.GetString("death-match-name-npc", ("name", MetaData(npc.NpcEnt).EntityName));
        }

        return string.Empty;
    }

    private string GetCalloutName(EntityUid source)
    {
        if (TryComp<ActorComponent>(source, out var actorComp))
        {
            return Loc.GetString("death-match-name-player",
                ("name", MetaData(source).EntityName),
                ("username", actorComp.PlayerSession.Name));
        }

        return Loc.GetString("death-match-name-npc", ("name", MetaData(source).EntityName));
    }
}
