using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Content.Server._DV.Cargo.Components;
using Content.Server._DV.Cargo.Systems;
using Content.Server._DV.Mail.Components;
using Content.Server.Access.Systems;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Damage.Components;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Server.Destructible.Thresholds.Triggers;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Radio.EntitySystems; // imp
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared._DV.CCVars;
using Content.Shared._DV.Mail;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Fluids.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.PDA;
using Content.Shared.Radio; // imp
using Content.Shared.Roles;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;


namespace Content.Server._DV.Mail.EntitySystems;

public sealed class MailSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly LogisticStatsSystem _logisticsStatsSystem = default!;
    [Dependency] private readonly RadioSystem _radioSystem = default!; // imp
    [Dependency] private readonly IGameTiming _timing = default!; // imp

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("mail");

        SubscribeLocalEvent<PlayerSpawningEvent>(OnSpawnPlayer, after: [typeof(SpawnPointSystem)]);

        SubscribeLocalEvent<MailComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<MailComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<MailComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<MailComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<MailComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<MailComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<MailComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<MailComponent, GotEmaggedEvent>(OnMailEmagged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MailTeleporterComponent>();

        while (query.MoveNext(out var uid, out var mailTeleporter))
        {
            if (TryComp<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered)
                continue;
            var curTime = _timing.CurTime;
            // imp. replacing the CCVar
            if (curTime < mailTeleporter.NextDelivery)
                continue;

            TimeSpan nextTimeToAdd;
            if (_random.Prob(0.5f)) // weight the result towards the average.
                nextTimeToAdd = _random.Next(mailTeleporter.MinInterval, mailTeleporter.MaxInterval);
            else
                nextTimeToAdd = mailTeleporter.AverageInterval;

            mailTeleporter.NextDelivery = _timing.CurTime + nextTimeToAdd;
            SpawnMail(uid, nextTimeToAdd, mailTeleporter);
        }
    }

    /// <summary>
    /// Dynamically add the MailReceiver component to appropriate entities.
    /// </summary>
    private void OnSpawnPlayer(PlayerSpawningEvent args)
    {
        if (args is { SpawnResult: { } spawnResult, Job: not null, Station: { } station }
            && HasComp<StationMailRouterComponent>(station))
        {
            AddComp<MailReceiverComponent>(spawnResult);
        }
    }

    private static void OnRemove(EntityUid uid, MailComponent component, ComponentRemove args)
    {
        component.PriorityCancelToken?.Cancel();
    }

    /// <summary>
    /// Try to open the mail.
    /// </summary>
    private void OnUseInHand(Entity<MailComponent> ent, ref UseInHandEvent args)
    {
        if (!ent.Comp.IsEnabled)
            return;
        if (ent.Comp.IsLocked)
        {
            _popup.PopupEntity(Loc.GetString("mail-locked"), ent, args.User);
            return;
        }
        OpenMail(ent, ent.Comp, args.User);
    }

    /// <summary>
    /// Handle logic similar between a normal mail unlock and an emag
    /// frying out the lock.
    /// </summary>
    private void UnlockMail(EntityUid uid, MailComponent component)
    {
        component.IsLocked = false;
        UpdateAntiTamperVisuals(uid, false);

        if (!component.IsPriority)
            return;

        // This is a successful delivery. Keep the failure timer from triggering.
        component.PriorityCancelToken?.Cancel();

        // The priority tape is visually considered to be a part of the
        // anti-tamper lock, so remove that too.
        _appearance.SetData(uid, MailVisuals.IsPriority, false);

        // The examination code depends on this being false to not show
        // the priority tape description anymore.
        component.IsPriority = false;
    }

    /// <summary>
    /// Check the ID against the mail's lock
    /// </summary>
    private void OnAfterInteractUsing(Entity<MailComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (!args.CanReach || !ent.Comp.IsLocked)
            return;

        if (!HasComp<AccessReaderComponent>(ent))
            return;

        IdCardComponent? idCard = null; // We need an ID card.

        if (HasComp<PdaComponent>(args.Used)) // Can we find it in a PDA if the user is using that?
        {
            _idCard.TryGetIdCard(args.Used, out var pdaId);
            idCard = pdaId;
        }
        if (idCard == null && HasComp<IdCardComponent>(args.Used)) // If we still don't have an ID, check if the item itself is one
            idCard = Comp<IdCardComponent>(args.Used);

        if (idCard == null) // Return if we still haven't found an id card.
            return;

        if (!HasComp<EmaggedComponent>(ent))
        {
            if (idCard.FullName != ent.Comp.Recipient || idCard.LocalizedJobTitle != ent.Comp.RecipientJob)
            {
                _popup.PopupEntity(Loc.GetString("mail-recipient-mismatch"), ent, args.User);
                return;
            }

            if (!_access.IsAllowed(ent, args.User))
            {
                _popup.PopupEntity(Loc.GetString("mail-invalid-access"), ent, args.User);
                return;
            }
        }

        // DeltaV - Add earnings to logistic stats
        ExecuteForEachLogisticsStats(ent,
            (station, _) =>
            {
                _logisticsStatsSystem.AddOpenedMailEarnings(station, ent.Comp.IsProfitable ? ent.Comp.Bounty : 0);
            });

        UnlockMail(ent, ent.Comp);

        if (!ent.Comp.IsProfitable)
        {
            _popup.PopupEntity(Loc.GetString("mail-unlocked"), ent, args.User);
            return;
        }

        _popup.PopupEntity(Loc.GetString("mail-unlocked-reward", ("bounty", ent.Comp.Bounty)), ent, args.User);
        ent.Comp.IsProfitable = false;

        var query = EntityQueryEnumerator<StationBankAccountComponent>();
        while (query.MoveNext(out var station, out var account))
        {
            if (_station.GetOwningStation(ent) != station)
                continue;

            _cargo.UpdateBankAccount(station, account, ent.Comp.Bounty);
        }
    }

    private void OnExamined(Entity<MailComponent> ent, ref ExaminedEvent args)
    {
        var mailEntityStrings = ent.Comp.IsLarge ? MailConstants.MailLarge : MailConstants.Mail;

        if (!args.IsInDetailsRange)
        {
            args.PushMarkup(Loc.GetString(mailEntityStrings.DescFar));
            return;
        }

        args.PushMarkup(Loc.GetString(mailEntityStrings.DescClose,
            ("name", ent.Comp.Recipient),
            ("job", ent.Comp.RecipientJob)));

        if (ent.Comp.IsFragile)
            args.PushMarkup(Loc.GetString("mail-desc-fragile"));

        if (ent.Comp.IsPriority)
            args.PushMarkup(Loc.GetString(ent.Comp.IsProfitable ? "mail-desc-priority" : "mail-desc-priority-inactive"));
    }


    /// <summary>
    /// Penalize a station for a failed delivery.
    /// </summary>
    /// <remarks>
    /// This will mark a parcel as no longer being profitable, which will
    /// prevent multiple failures on different conditions for the same
    /// delivery.
    ///
    /// The standard penalization is breaking the anti-tamper lock,
    /// but this allows a delivery to fail for other reasons too
    /// while having a generic function to handle different messages.
    /// </remarks>
    private void PenalizeStationFailedDelivery(Entity<MailComponent> ent, string localizationString)
    {
        if (!ent.Comp.IsProfitable)
            return;

        _chat.TrySendInGameICMessage(ent, Loc.GetString(localizationString, ("credits", ent.Comp.Penalty)), InGameICChatType.Speak, false);
        _audio.PlayPvs(ent.Comp.PenaltySound, ent);

        ent.Comp.IsProfitable = false;

        if (ent.Comp.IsPriority)
            _appearance.SetData(ent, MailVisuals.IsPriorityInactive, true);

        var query = EntityQueryEnumerator<StationBankAccountComponent>();
        while (query.MoveNext(out var station, out var account))
        {
            if (_station.GetOwningStation(ent) != station)
                continue;

            _cargo.UpdateBankAccount(station, account, ent.Comp.Penalty);
            return;
        }
    }

    private void OnDestruction(Entity<MailComponent> ent, ref DestructionEventArgs args)
    {
        if (ent.Comp.IsLocked)
        {
            // DeltaV - Tampered mail recorded to logistic stats
            ExecuteForEachLogisticsStats(ent,
                (station, logisticStats) =>
                {
                    _logisticsStatsSystem.AddTamperedMailLosses(station,
                        logisticStats,
                        ent.Comp.IsProfitable ? ent.Comp.Penalty : 0);
                });

            PenalizeStationFailedDelivery(ent, "mail-penalty-lock");
        }

        if (ent.Comp.IsEnabled)
            OpenMail(ent);

        UpdateAntiTamperVisuals(ent, false);
    }

    private void OnDamage(Entity<MailComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
            return;

        if (!_container.TryGetContainer(ent, "contents", out var contents))
            return;

        // Transfer damage to the contents.
        // This should be a general-purpose feature for all containers in the future.
        foreach (var entity in contents.ContainedEntities.ToArray())
        {
            _damageable.TryChangeDamage(entity, args.DamageDelta);
        }
    }

    private void OnBreak(Entity<MailComponent> ent, ref BreakageEventArgs args)
    {
        _appearance.SetData(ent, MailVisuals.IsBroken, true);

            if (!ent.Comp.IsFragile)
                return;
            // DeltaV - Broken mail recorded to logistic stats
            ExecuteForEachLogisticsStats(ent,
                (station, logisticStats) =>
                {
                    _logisticsStatsSystem.AddDamagedMailLosses(station,
                        logisticStats,
                        ent.Comp.IsProfitable ? ent.Comp.Penalty : 0);
                });

        PenalizeStationFailedDelivery(ent, "mail-penalty-fragile");
    }

    private void OnMailEmagged(Entity<MailComponent> ent, ref GotEmaggedEvent args)
    {
        if (!ent.Comp.IsLocked)
            return;

        UnlockMail(ent, ent.Comp);

        _popup.PopupEntity(Loc.GetString("mail-unlocked-by-emag"), ent, args.UserUid);

        _audio.PlayPvs(ent.Comp.EmagSound, ent, AudioParams.Default.WithVolume(4));
        ent.Comp.IsProfitable = false;
        args.Handled = true;
    }

    /// <summary>
    /// Returns true if the given entity is considered fragile for delivery.
    /// </summary>
    private bool IsEntityFragile(EntityUid uid, int fragileDamageThreshold)
    {
        // It takes damage on falling.
        if (HasComp<DamageOnLandComponent>(uid))
            return true;

        // It can be spilled easily and has something to spill.
        if (HasComp<SpillableComponent>(uid)
            && TryComp<OpenableComponent>(uid, out var openable)
            && !_openable.IsClosed(uid, null, openable)
            && _solution.PercentFull(uid) > 0)
            return true;

        // It might be made of non-reinforced glass.
        if (TryComp<DamageableComponent>(uid, out var damageableComponent)
            && damageableComponent.DamageModifierSetId == "Glass")
            return true;

        // Fallback: It breaks or is destroyed in less than a damage
        // threshold dictated by the teleporter.
        if (!TryComp<DestructibleComponent>(uid, out var destructibleComp))
            return false;

        foreach (var threshold in destructibleComp.Thresholds)
        {
            if (threshold.Trigger is not DamageTrigger trigger || trigger.Damage >= fragileDamageThreshold)
                continue;

            foreach (var behavior in threshold.Behaviors)
            {
                if (behavior is not DoActsBehavior doActs)
                    continue;

                if (doActs.Acts.HasFlag(ThresholdActs.Breakage) || doActs.Acts.HasFlag(ThresholdActs.Destruction))
                    return true;
            }
        }

        return false;
    }

    private bool TryMatchJobTitleToDepartment(string jobTitle, [NotNullWhen(true)] out string? jobDepartment)
    {
        jobDepartment = null;

        var departments = _prototypeManager.EnumeratePrototypes<DepartmentPrototype>();

        foreach (var department in departments)
        {
            var foundJob = department.Roles
                .Any(role =>
                    _prototypeManager.TryIndex(role, out var jobPrototype)
                    && jobPrototype.LocalizedName == jobTitle);

            if (!foundJob)
                continue;

            jobDepartment = department.ID;
            return true;
        }

        return false;
    }

    private bool TryMatchJobTitleToPrototype(string jobTitle, [NotNullWhen(true)] out JobPrototype? jobPrototype)
    {
        jobPrototype = _prototypeManager
            .EnumeratePrototypes<JobPrototype>()
            .FirstOrDefault(job => job.LocalizedName == jobTitle);

        return jobPrototype != null;
    }

    /// <summary>
    /// Handle all the gritty details particular to a new mail entity.
    /// </summary>
    /// <remarks>
    /// This is separate mostly so the unit tests can get to it.
    /// </remarks>
    public void SetupMail(EntityUid uid, MailTeleporterComponent component, MailRecipient recipient)
    {
        var mailComp = EnsureComp<MailComponent>(uid);

        var container = _container.EnsureContainer<Container>(uid, "contents");
        foreach (var entity in EntitySpawnCollection.GetSpawns(mailComp.Contents, _random).Select(item => EntityManager.SpawnEntity(item, Transform(uid).Coordinates)))
        {
            if (!_container.Insert(entity, container))
            {
                _sawmill.Error($"Can't insert {ToPrettyString(entity)} into new mail delivery {ToPrettyString(uid)}! Deleting it.");
                QueueDel(entity);
            }
            else if (!mailComp.IsFragile && IsEntityFragile(entity, _config.GetCVar(DCCVars.MailFragileDamageThreshold)))
            {
                mailComp.IsFragile = true;
            }
        }

        if (_random.Prob(_config.GetCVar(DCCVars.MailPriorityChances)))
            mailComp.IsPriority = true;

        // This needs to override both the random probability and the
        // entity prototype, so this is fine.
        if (!recipient.MayReceivePriorityMail)
            mailComp.IsPriority = false;

        mailComp.RecipientJob = recipient.Job;
        mailComp.Recipient = recipient.Name;

        // Imp: Set base bounty and penalty
        mailComp.Bounty += _config.GetCVar(DCCVars.MailDefaultBounty);
        mailComp.Penalty += _config.GetCVar(DCCVars.MailDefaultPenelty);

        // Frontier: Large mail bonus
        var mailEntityStrings = mailComp.IsLarge ? MailConstants.MailLarge : MailConstants.Mail;
        if (mailComp.IsLarge)
        {
            mailComp.Bounty += _config.GetCVar(DCCVars.MailLargeBonus);
            mailComp.Penalty += _config.GetCVar(DCCVars.MailLargeMalus);
        }
        // End Frontier

        if (mailComp.IsFragile)
        {
            mailComp.Bounty += _config.GetCVar(DCCVars.MailFragileBonus);
            mailComp.Penalty += _config.GetCVar(DCCVars.MailFragileMalus);
            _appearance.SetData(uid, MailVisuals.IsFragile, true);
        }

        if (mailComp.IsPriority)
        {
            mailComp.Bounty +=  _config.GetCVar(DCCVars.MailPriorityBonus);
            mailComp.Penalty += _config.GetCVar(DCCVars.MailPriorityMalus);
            _appearance.SetData(uid, MailVisuals.IsPriority, true);

            mailComp.PriorityCancelToken = new CancellationTokenSource();

                Timer.Spawn((int) TimeSpan.FromMinutes(_config.GetCVar(DCCVars.MailPriorityDuration)).TotalMilliseconds,
                    () =>
                    {
                        // DeltaV - Expired mail recorded to logistic stats
                        ExecuteForEachLogisticsStats(uid,
                            (station, logisticStats) =>
                        {
                            _logisticsStatsSystem.AddExpiredMailLosses(station,
                                logisticStats,
                                mailComp.IsProfitable ? mailComp.Penalty : 0);
                        });
                    PenalizeStationFailedDelivery((uid, mailComp), "mail-penalty-expired");
                },
                mailComp.PriorityCancelToken.Token);
        }

        _appearance.SetData(uid, MailVisuals.JobIcon, recipient.JobIcon);

        _metaData.SetEntityName(uid,
            Loc.GetString(mailEntityStrings.NameAddressed, // Frontier: move constant to MailEntityString
                ("recipient", recipient.Name)));

        var accessReader = EnsureComp<AccessReaderComponent>(uid);
        foreach (var access in recipient.AccessTags)
        {
            accessReader.AccessLists.Add([access]);
        }
    }

    /// <summary>
    /// Return the parcels waiting for delivery.
    /// </summary>
    /// <param name="uid">The mail teleporter to check.</param>
    private List<EntityUid> GetUndeliveredParcels(EntityUid uid)
    {
        // An alternative solution would be to keep a list of the unopened
        // parcels spawned by the teleporter and see if they're not carried
        // by someone, but this is simple, and simple is good.
        var coordinates = Transform(uid).Coordinates;
        const LookupFlags lookupFlags = LookupFlags.Dynamic | LookupFlags.Sundries;

        var entitiesInTile = _lookup.GetEntitiesIntersecting(coordinates, lookupFlags);

        return entitiesInTile.Where(HasComp<MailComponent>).ToList();
    }

    /// <summary>
    /// Return how many parcels are waiting for delivery.
    /// </summary>
    /// <param name="uid">The mail teleporter to check.</param>
    private uint GetUndeliveredParcelCount(EntityUid uid)
    {
        return (uint)GetUndeliveredParcels(uid).Count;
    }

    /// <summary>
    /// Try to match a mail receiver to a mail teleporter.
    /// </summary>
    public bool TryGetMailTeleporterForReceiver(EntityUid receiverUid, [NotNullWhen(true)] out MailTeleporterComponent? teleporterComponent, [NotNullWhen(true)] out EntityUid? teleporterUid)
    {
        var query = EntityQueryEnumerator<MailTeleporterComponent>();
        var receiverStation = _station.GetOwningStation(receiverUid);

        while (query.MoveNext(out var uid, out var mailTeleporter))
        {
            var teleporterStation = _station.GetOwningStation(uid);
            if (receiverStation != teleporterStation)
                continue;
            teleporterComponent = mailTeleporter;
            teleporterUid = uid;
            return true;
        }

        teleporterComponent = null;
        teleporterUid = null;
        return false;
    }

    /// <summary>
    /// Try to construct a recipient struct for a mail parcel based on a receiver.
    /// </summary>
    public bool TryGetMailRecipientForReceiver(EntityUid receiverUid, [NotNullWhen(true)] out MailRecipient? recipient)
    {
        if (_idCard.TryFindIdCard(receiverUid, out var idCard)
            && TryComp<AccessComponent>(idCard.Owner, out var access)
            && idCard.Comp.FullName != null)
        {
            var accessTags = access.Tags;
            var mayReceivePriorityMail = !(_mind.GetMind(receiverUid) == null);

            recipient = new MailRecipient(
                idCard.Comp.FullName,
                idCard.Comp.LocalizedJobTitle ?? idCard.Comp.JobTitle ?? "Unknown",
                idCard.Comp.JobIcon,
                accessTags,
                mayReceivePriorityMail);

            return true;
        }

        recipient = null;
        return false;
    }

    /// <summary>
    /// Get the list of valid mail recipients for a mail teleporter.
    /// </summary>
    private List<MailRecipient> GetMailRecipientCandidates(EntityUid uid)
    {
        var candidateList = new List<MailRecipient>();
        var query = EntityQueryEnumerator<MailReceiverComponent>();
        var teleporterStation = _station.GetOwningStation(uid);

        while (query.MoveNext(out var receiverUid, out _))
        {
            var receiverStation = _station.GetOwningStation(receiverUid);
            if (receiverStation != teleporterStation)
                continue;

            if (TryGetMailRecipientForReceiver(receiverUid, out var recipient))
                candidateList.Add(recipient.Value);
        }

        return candidateList;
    }

    /// <summary>
    /// Handle the spawning of all the mail for a mail teleporter.
    /// </summary>
    private void SpawnMail(EntityUid uid, TimeSpan nextDelivery, MailTeleporterComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            _sawmill.Error($"Tried to SpawnMail on {ToPrettyString(uid)} without a valid MailTeleporterComponent!");
            return;
        }

        if (GetUndeliveredParcelCount(uid) >= _config.GetCVar(DCCVars.MailMaximumUndeliveredParcels))
            return;

        var candidateList = GetMailRecipientCandidates(uid);

        if (candidateList.Count <= 0)
        {
            _sawmill.Error("List of mail candidates was empty!");
            return;
        }

        if (!_prototypeManager.TryIndex<MailDeliveryPoolPrototype>(component.MailPool, out var pool))
        {
            _sawmill.Error($"Can't index {ToPrettyString(uid)}'s MailPool {component.MailPool}!");
            return;
        }

        var deliveryCount = 1 + candidateList.Count / _config.GetCVar(DCCVars.MailCandidatesPerDelivery);
        List<string> chosenParcels = [];
        for (var i = 0; i < deliveryCount; i++)
        {
            var candidate = _random.Pick(candidateList);
            var possibleParcels = new Dictionary<String, float>(pool.Everyone);

            if (TryMatchJobTitleToPrototype(candidate.Job, out var jobPrototype)
                && pool.Jobs.TryGetValue(jobPrototype.ID, out var jobParcels))
            {
                possibleParcels = possibleParcels
                    .Concat(jobParcels)
                    .GroupBy(g => g.Key)
                    .ToDictionary(pair => pair.Key, pair => pair.First().Value);
            }

            if (TryMatchJobTitleToDepartment(candidate.Job, out var department)
                && pool.Departments.TryGetValue(department, out var departmentParcels))
            {
                possibleParcels = possibleParcels
                    .Concat(departmentParcels)
                    .GroupBy(g => g.Key)
                    .ToDictionary(pair => pair.Key, pair => pair.First().Value);
            }

            var accumulated = 0f;
            var randomPoint = _random.NextFloat(possibleParcels.Values.Sum());
            string? chosenParcel = null;

            foreach (var parcel in possibleParcels)
            {
                accumulated += parcel.Value;
                if (!(accumulated >= randomPoint))
                    continue;
                chosenParcel = parcel.Key;
                break;
            }

            if (chosenParcel == null)
            {
                _sawmill.Error($"MailSystem wasn't able to find a deliverable parcel for {candidate.Name}, {candidate.Job}!");
                return;
            }

            var coordinates = Transform(uid).Coordinates;
            var mail = EntityManager.SpawnEntity(chosenParcel, coordinates);
            SetupMail(mail, component, candidate);

            _tag.AddTag(mail, "Mail"); // Frontier
        }

        if (_container.TryGetContainer(uid, "queued", out var queued))
            _container.EmptyContainer(queued);

        // Spawn VFX
        Spawn(component.BeamInFx, Transform(uid).Coordinates);

        _audio.PlayPvs(component.TeleportSound, uid);

        if (component.RadioNotification) // imp
            Report(uid, component.RadioChannel, component.ShipmentRecievedMessage, ("timeLeft", Math.Round(nextDelivery.TotalMinutes)));
    }

    private void Report(EntityUid source, string channelName, string messageKey, params (string, object)[] args) // imp
    {
        var message = args.Length == 0 ? Loc.GetString(messageKey) : Loc.GetString(messageKey, args);
        var channel = _prototypeManager.Index<RadioChannelPrototype>(channelName);
        _radioSystem.SendRadioMessage(source, message, channel, source);
    }

    private void OpenMail(EntityUid uid, MailComponent? component = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _audio.PlayPvs(component.OpenSound, uid);

        if (user != null)
            _hands.TryDrop((EntityUid) user);

        if (!_container.TryGetContainer(uid, "contents", out var contents))
        {
            // I silenced this error because it fails non deterministically in tests and doesn't seem to effect anything else.
            // _sawmill.Error($"Mail {ToPrettyString(uid)} was missing contents container!");
            return;
        }

        foreach (var entity in contents.ContainedEntities.ToArray())
        {
            _hands.PickupOrDrop(user, entity);
        }

        _tag.AddTag(uid, "Trash");
        _tag.AddTag(uid, "Recyclable");
        component.IsEnabled = false;
        UpdateMailTrashState(uid, true);
    }

    private void UpdateAntiTamperVisuals(EntityUid uid, bool isLocked)
    {
        _appearance.SetData(uid, MailVisuals.IsLocked, isLocked);
    }

    private void UpdateMailTrashState(EntityUid uid, bool isTrash)
    {
        _appearance.SetData(uid, MailVisuals.IsTrash, isTrash);
    }

    // DeltaV - Helper function that executes for each StationLogisticsStatsComponent
    // For updating MailMetrics stats
    private void ExecuteForEachLogisticsStats(EntityUid uid,
        Action<EntityUid, StationLogisticStatsComponent> action)
    {

        var query = EntityQueryEnumerator<StationLogisticStatsComponent>();
        while (query.MoveNext(out var station, out var logisticStats))
        {
            if (_station.GetOwningStation(uid) != station)
                continue;
            action(station, logisticStats);
        }
    }
}

public struct MailRecipient(
    string name,
    string job,
    string jobIcon,
    HashSet<ProtoId<AccessLevelPrototype>> accessTags,
    bool mayReceivePriorityMail)
{
    public readonly string Name = name;
    public readonly string Job = job;
    public readonly string JobIcon = jobIcon;
    public readonly HashSet<ProtoId<AccessLevelPrototype>> AccessTags = accessTags;
    public readonly bool MayReceivePriorityMail = mayReceivePriorityMail;
}
