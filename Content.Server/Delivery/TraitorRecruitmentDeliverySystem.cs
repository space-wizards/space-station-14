using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Roles;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.Delivery;
using Content.Shared.FingerprintReader;
using Content.Shared.Forensics.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Paper;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Systems;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Delivery;

/// <summary>
/// Turns the addressed recipient of a <see cref="TraitorRecruitmentDeliveryComponent"/>
/// letter into a traitor when they open it, writes the reused traitor briefing
/// onto the enclosed paper and arms its self-destruct.
/// </summary>
public sealed partial class TraitorRecruitmentDeliverySystem : EntitySystem
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private RoleSystem _roles = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private FingerprintReaderSystem _fingerprint = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private TriggerSystem _trigger = default!;
    [Dependency] private StationRecordsSystem _records = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private GameTicker _gameTicker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TraitorRecruitmentDeliveryComponent, DeliveryOpenedEvent>(OnOpened);
        SubscribeLocalEvent<TraitorRecruitmentDeliveryComponent, DeliverySelectRecipientEvent>(OnSelectRecipient);
    }

    private void OnSelectRecipient(
        Entity<TraitorRecruitmentDeliveryComponent> ent,
        ref DeliverySelectRecipientEvent args)
    {
        if (args.Cancelled || args.Recipient != null)
            return;

        if (GetOrCreateRule(ent.Comp.RulePrototype) is not { } rule)
        {
            args.Cancelled = true;
            return;
        }

        var recordsByFingerprint = new Dictionary<string, GeneralStationRecord>();
        foreach (var (_, record) in _records.GetRecordsOfType<GeneralStationRecord>(args.Station))
        {
            if (record.Fingerprint == null)
                continue;

            recordsByFingerprint.TryAdd(record.Fingerprint, record);
        }

        if (recordsByFingerprint.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        var candidates = new List<GeneralStationRecord>();
        foreach (var session in _player.Sessions)
        {
            if (session.AttachedEntity is not { } playerEnt)
                continue;

            if (_station.GetOwningStation(playerEnt) != args.Station)
                continue;

            if (!_mind.TryGetMind(playerEnt, out _, out _))
                continue;

            if (!_antag.CanBeAntag(session, rule, ent.Comp.AntagProto, checkPref: true))
                continue;

            if (!TryComp<FingerprintComponent>(playerEnt, out var fingerprint) ||
                fingerprint.Fingerprint == null)
            {
                continue;
            }

            if (recordsByFingerprint.TryGetValue(fingerprint.Fingerprint, out var record))
                candidates.Add(record);
        }

        if (candidates.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        args.Recipient = _random.Pick(candidates);
    }

    private void OnOpened(Entity<TraitorRecruitmentDeliveryComponent> ent, ref DeliveryOpenedEvent args)
    {
        if (!TryComp<DeliveryComponent>(ent, out var delivery) ||
            !_container.TryGetContainer(ent, delivery.Container, out var container))
            return;

        EntityUid? paper = null;
        foreach (var contained in container.ContainedEntities)
        {
            if (HasComp<PaperComponent>(contained))
            {
                paper = contained;
                break;
            }
        }

        var user = args.User;

        // Match the delivery's fingerprint lock instead of storing separate recipient identity.
        var legit = !delivery.WasPenalized
                    && TryComp<FingerprintReaderComponent>(ent, out var reader)
                    && reader.AllowedFingerprints.Count > 0
                    && _fingerprint.IsAllowed((ent.Owner, reader), user, out _, showPopup: false, checkGloves: false);

        if (!legit || !TryRecruit(user, ent.Comp, out var briefing))
        {
            if (paper != null)
                IgnitePaper(paper.Value, user);
            return;
        }

        if (paper != null)
        {
            var content = Loc.GetString("traitor-by-mail-body", ("briefing", briefing));
            _paper.SetContent(paper.Value, content);

            _trigger.ActivateTimerTrigger(paper.Value, user);
        }
    }

    private void IgnitePaper(EntityUid paper, EntityUid user)
    {
        if (!TryComp<TimerTriggerComponent>(paper, out var timer) ||
            timer.KeyOut is not { } key)
        {
            return;
        }

        _trigger.Trigger(paper, user, key, predicted: false);
    }

    private bool TryRecruit(EntityUid user, TraitorRecruitmentDeliveryComponent comp, out string briefing)
    {
        briefing = string.Empty;

        if (!TryComp<ActorComponent>(user, out var actor))
            return false;

        if (!_mind.TryGetMind(user, out var mindId, out _))
            return false;

        if (GetOrCreateRule(comp.RulePrototype) is not { } rule)
            return false;

        if (!_antag.TryMakeAntag(rule, comp.AntagProto, actor.PlayerSession, checkPref: true))
            return false;

        briefing = _roles.MindGetBriefing(mindId) ?? string.Empty;
        return true;
    }

    private Entity<AntagSelectionComponent>? GetOrCreateRule(EntProtoId rulePrototype)
    {
        var query = EntityQueryEnumerator<TraitorRuleComponent, AntagSelectionComponent>();
        while (query.MoveNext(out var uid, out _, out var antag))
        {
            if (MetaData(uid).EntityPrototype?.ID != rulePrototype.Id ||
                HasComp<EndedGameRuleComponent>(uid))
            {
                continue;
            }

            if (!_gameTicker.IsGameRuleActive(uid) && !_gameTicker.StartGameRule(uid))
                return null;

            return (uid, antag);
        }

        var rule = _gameTicker.AddGameRule(rulePrototype);
        if (!HasComp<TraitorRuleComponent>(rule) ||
            !TryComp<AntagSelectionComponent>(rule, out var antagComp) ||
            !_gameTicker.StartGameRule(rule))
        {
            QueueDel(rule);
            return null;
        }

        return (rule, antagComp);
    }
}
