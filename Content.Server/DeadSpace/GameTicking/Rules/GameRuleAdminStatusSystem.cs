// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using System.Text;
using Content.Server.Chat.Managers;
using Content.Shared.GameTicking;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Collects read-only status sections from every active game rule and sends one combined admin report.
/// </summary>
public sealed class GameRuleAdminStatusSystem : EntitySystem
{
    private static readonly TimeSpan ReportInterval = TimeSpan.FromMinutes(1);

    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private TimeSpan? _nextReport;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundStarted(RoundStartedEvent args)
    {
        _nextReport = _timing.CurTime + ReportInterval;
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _nextReport = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_nextReport == null || _timing.CurTime < _nextReport)
            return;

        _nextReport = _timing.CurTime + ReportInterval;
        SendReport();
    }

    private void SendReport()
    {
        var sections = new List<GameRuleAdminStatusSection>();

        foreach (var rule in _ticker.GetActiveGameRules())
        {
            try
            {
                var ev = new CollectGameRuleAdminStatusEvent(rule);
                RaiseLocalEvent(rule, ev, true);
                sections.AddRange(ev.Sections);
            }
            catch (Exception exception)
            {
                Log.Error($"Failed to collect admin status for game rule {ToPrettyString(rule)}: {exception}");
            }
        }

        if (sections.Count == 0)
            return;

        sections.Sort((left, right) =>
        {
            var titleComparison = string.Compare(left.Title, right.Title, StringComparison.Ordinal);
            return titleComparison != 0
                ? titleComparison
                : left.Rule.CompareTo(right.Rule);
        });

        var duplicateTitles = sections
            .GroupBy(section => section.Title)
            .ToDictionary(group => group.Key, group => group.Count());

        var report = new StringBuilder(Loc.GetString("game-rule-admin-status-header"));
        foreach (var section in sections)
        {
            var title = duplicateTitles[section.Title] > 1
                ? Loc.GetString(
                    "game-rule-admin-status-duplicate-title",
                    ("title", section.Title),
                    ("rule", ToPrettyString(section.Rule)))
                : section.Title;

            report.AppendLine();
            report.AppendLine(Loc.GetString("game-rule-admin-status-section", ("title", title)));
            foreach (var line in section.Lines)
                report.AppendLine(line);
        }

        _chat.SendAdminAnnouncement(report.ToString().TrimEnd());
    }
}

/// <summary>
/// Directed event raised on each active rule entity.
/// </summary>
public sealed class CollectGameRuleAdminStatusEvent : EntityEventArgs
{
    public EntityUid Rule { get; }
    public List<GameRuleAdminStatusSection> Sections { get; } = new();

    public CollectGameRuleAdminStatusEvent(EntityUid rule)
    {
        Rule = rule;
    }

    public void AddSection(string title, IEnumerable<string> lines)
    {
        var filtered = lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        if (string.IsNullOrWhiteSpace(title) || filtered.Length == 0)
            return;

        Sections.Add(new GameRuleAdminStatusSection(Rule, title, filtered));
    }

    public void AddSection(string title, params string[] lines)
    {
        AddSection(title, (IEnumerable<string>) lines);
    }
}

public sealed record GameRuleAdminStatusSection(
    EntityUid Rule,
    string Title,
    IReadOnlyList<string> Lines);
