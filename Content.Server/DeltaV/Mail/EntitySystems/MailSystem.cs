using Content.Server.Access.Systems;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Damage.Components;
using Content.Server.DeltaV.Cargo.Components;
using Content.Server.DeltaV.Cargo.Systems;
using Content.Server.DeltaV.Mail.Components;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Server.Destructible.Thresholds.Triggers;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Access;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.DeltaV.Mail;
using Content.Shared.Destructible;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Fluids.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.DeltaV.Mail.EntitySystems
{
    public sealed class MailSystem : EntitySystem
    {
        [Dependency] private readonly AccessReaderSystem _accessSystem = default!;
        [Dependency] private readonly CargoSystem _cargoSystem = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IdCardSystem _idCardSystem = default!;
        [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
        [Dependency] private readonly MindSystem _mindSystem = default!;
        [Dependency] private readonly OpenableSystem _openable = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;

        // DeltaV - system that keeps track of mail and cargo stats
        [Dependency] private readonly LogisticStatsSystem _logisticsStatsSystem = default!;

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("mail");

            SubscribeLocalEvent<PlayerSpawningEvent>(OnSpawnPlayer, after: new[] { typeof(SpawnPointSystem) });

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

                mailTeleporter.Accumulator += frameTime;

                if (mailTeleporter.Accumulator < mailTeleporter.TeleportInterval.TotalSeconds)
                    continue;

                mailTeleporter.Accumulator -= (float)mailTeleporter.TeleportInterval.TotalSeconds;
                SpawnMail(uid, mailTeleporter);
            }
        }

        /// <summary>
        /// Dynamically add the MailReceiver component to appropriate entities.
        /// </summary>
        private void OnSpawnPlayer(PlayerSpawningEvent args)
        {
            if (args.SpawnResult == null ||
                args.Job == null ||
                args.Station is not {} station)
            {
                return;
            }

            if (!HasComp<StationMailRouterComponent>(station))
                return;

            AddComp<MailReceiverComponent>(args.SpawnResult.Value);
        }

        private static void OnRemove(EntityUid uid, MailComponent component, ComponentRemove args)
        {
            component.PriorityCancelToken?.Cancel();
        }

        /// <summary>
        /// Try to open the mail.
        /// </summary>
        private void OnUseInHand(EntityUid uid, MailComponent component, UseInHandEvent args)
        {
            if (!component.IsEnabled)
                return;
            if (component.IsLocked)
            {
                _popupSystem.PopupEntity(Loc.GetString("mail-locked"), uid, args.User);
                return;
            }
            OpenMail(uid, component, args.User);
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
                _appearanceSystem.SetData(uid, MailVisuals.IsPriority, false);

                // The examination code depends on this being false to not show
                // the priority tape description anymore.
                component.IsPriority = false;
        }

        /// <summary>
        /// Check the ID against the mail's lock
        /// </summary>
        private void OnAfterInteractUsing(EntityUid uid, MailComponent component, AfterInteractUsingEvent args)
        {
            if (!args.CanReach || !component.IsLocked)
                return;

            if (!HasComp<AccessReaderComponent>(uid))
                return;

            IdCardComponent? idCard = null; // We need an ID card.

            if (HasComp<PdaComponent>(args.Used)) // Can we find it in a PDA if the user is using that?
            {
                _idCardSystem.TryGetIdCard(args.Used, out var pdaId);
                idCard = pdaId;
            }
            if (idCard == null && HasComp<IdCardComponent>(args.Used)) // If we still don't have an ID, check if the item itself is one
                idCard = Comp<IdCardComponent>(args.Used);

            if (idCard == null) // Return if we still haven't found an id card.
                return;

            if (!HasComp<EmaggedComponent>(uid))
            {
                if (idCard.FullName != component.Recipient || idCard.LocalizedJobTitle != component.RecipientJob)
                {
                    _popupSystem.PopupEntity(Loc.GetString("mail-recipient-mismatch"), uid, args.User);
                    return;
                }

                if (!_accessSystem.IsAllowed(uid, args.User))
                {
                    _popupSystem.PopupEntity(Loc.GetString("mail-invalid-access"), uid, args.User);
                    return;
                }
            }

            // DeltaV - Add earnings to logistic stats
            ExecuteForEachLogisticsStats(uid,
                (station, logisticStats) =>
            {
                _logisticsStatsSystem.AddOpenedMailEarnings(station,
                    logisticStats,
                    component.IsProfitable ? component.Bounty : 0);
            });

            UnlockMail(uid, component);

            if (!component.IsProfitable)
            {
                _popupSystem.PopupEntity(Loc.GetString("mail-unlocked"), uid, args.User);
                return;
            }

            _popupSystem.PopupEntity(Loc.GetString("mail-unlocked-reward", ("bounty", component.Bounty)), uid, args.User);
            component.IsProfitable = false;

            var query = EntityQueryEnumerator<StationBankAccountComponent>();
            while (query.MoveNext(out var station, out var account))
            {
                if (_stationSystem.GetOwningStation(uid) != station)
                    continue;

                _cargoSystem.UpdateBankAccount(station, account, component.Bounty);
            }
        }

        private void OnExamined(EntityUid uid, MailComponent component, ExaminedEvent args)
        {
            var mailEntityStrings = component.IsLarge ? MailConstants.MailLarge : MailConstants.Mail;

            if (!args.IsInDetailsRange)
            {
                args.PushMarkup(Loc.GetString(mailEntityStrings.DescFar));
                return;
            }

            args.PushMarkup(Loc.GetString(mailEntityStrings.DescClose,
                ("name", component.Recipient),
                ("job", component.RecipientJob)));

            if (component.IsFragile)
                args.PushMarkup(Loc.GetString("mail-desc-fragile"));

            if (component.IsPriority)
                args.PushMarkup(Loc.GetString(component.IsProfitable ? "mail-desc-priority" : "mail-desc-priority-inactive"));
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
        private void PenalizeStationFailedDelivery(EntityUid uid, MailComponent component, string localizationString)
        {
            if (!component.IsProfitable)
                return;

            _chatSystem.TrySendInGameICMessage(uid, Loc.GetString(localizationString, ("credits", component.Penalty)), InGameICChatType.Speak, false);
            _audioSystem.PlayPvs(component.PenaltySound, uid);

            component.IsProfitable = false;

            if (component.IsPriority)
                _appearanceSystem.SetData(uid, MailVisuals.IsPriorityInactive, true);

            var query = EntityQueryEnumerator<StationBankAccountComponent>();
            while (query.MoveNext(out var station, out var account))
            {
                if (_stationSystem.GetOwningStation(uid) != station)
                    continue;

                _cargoSystem.UpdateBankAccount(station, account, component.Penalty);
                return;
            }
        }

        private void OnDestruction(EntityUid uid, MailComponent component, DestructionEventArgs args)
        {
            if (component.IsLocked)
            {
                // DeltaV - Tampered mail recorded to logistic stats
                ExecuteForEachLogisticsStats(uid,
                    (station, logisticStats) =>
                {
                    _logisticsStatsSystem.AddTamperedMailLosses(station,
                        logisticStats,
                        component.IsProfitable ? component.Penalty : 0);
                });

                PenalizeStationFailedDelivery(uid, component, "mail-penalty-lock");
            }

            if (component.IsEnabled)
                OpenMail(uid, component);

            UpdateAntiTamperVisuals(uid, false);
        }

        private void OnDamage(EntityUid uid, MailComponent component, DamageChangedEvent args)
        {
            if (args.DamageDelta == null)
                return;

            if (!_containerSystem.TryGetContainer(uid, "contents", out var contents))
                return;

            // Transfer damage to the contents.
            // This should be a general-purpose feature for all containers in the future.
            foreach (var entity in contents.ContainedEntities.ToArray())
            {
                _damageableSystem.TryChangeDamage(entity, args.DamageDelta);
            }
        }

        private void OnBreak(EntityUid uid, MailComponent component, BreakageEventArgs args)
        {
            _appearanceSystem.SetData(uid, MailVisuals.IsBroken, true);

            if (!component.IsFragile)
                return;
            // DeltaV - Broken mail recorded to logistic stats
            ExecuteForEachLogisticsStats(uid,
                (station, logisticStats) =>
                {
                    _logisticsStatsSystem.AddDamagedMailLosses(station,
                        logisticStats,
                        component.IsProfitable ? component.Penalty : 0);
                });

            PenalizeStationFailedDelivery(uid, component, "mail-penalty-fragile");
        }

        private void OnMailEmagged(EntityUid uid, MailComponent component, ref GotEmaggedEvent args)
        {
            if (!component.IsLocked)
                return;

            UnlockMail(uid, component);

            _popupSystem.PopupEntity(Loc.GetString("mail-unlocked-by-emag"), uid, args.UserUid);

            _audioSystem.PlayPvs(component.EmagSound, uid, AudioParams.Default.WithVolume(4));
            component.IsProfitable = false;
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

            var container = _containerSystem.EnsureContainer<Container>(uid, "contents");
            foreach (var entity in EntitySpawnCollection.GetSpawns(mailComp.Contents, _random).Select(item => EntityManager.SpawnEntity(item, Transform(uid).Coordinates)))
            {
                if (!_containerSystem.Insert(entity, container))
                {
                    _sawmill.Error($"Can't insert {ToPrettyString(entity)} into new mail delivery {ToPrettyString(uid)}! Deleting it.");
                    QueueDel(entity);
                }
                else if (!mailComp.IsFragile && IsEntityFragile(entity, component.FragileDamageThreshold))
                {
                    mailComp.IsFragile = true;
                }
            }

            if (_random.Prob(component.PriorityChance))
                mailComp.IsPriority = true;

            // This needs to override both the random probability and the
            // entity prototype, so this is fine.
            if (!recipient.MayReceivePriorityMail)
                mailComp.IsPriority = false;

            mailComp.RecipientJob = recipient.Job;
            mailComp.Recipient = recipient.Name;

            // Frontier: Large mail bonus
            var mailEntityStrings = mailComp.IsLarge ? MailConstants.MailLarge : MailConstants.Mail;
            if (mailComp.IsLarge)
            {
                mailComp.Bounty += component.LargeBonus;
                mailComp.Penalty += component.LargeMalus;
            }
            // End Frontier

            if (mailComp.IsFragile)
            {
                mailComp.Bounty += component.FragileBonus;
                mailComp.Penalty += component.FragileMalus;
                _appearanceSystem.SetData(uid, MailVisuals.IsFragile, true);
            }

            if (mailComp.IsPriority)
            {
                mailComp.Bounty += component.PriorityBonus;
                mailComp.Penalty += component.PriorityMalus;
                _appearanceSystem.SetData(uid, MailVisuals.IsPriority, true);

                mailComp.PriorityCancelToken = new CancellationTokenSource();

                Timer.Spawn((int) component.PriorityDuration.TotalMilliseconds,
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

                        PenalizeStationFailedDelivery(uid, mailComp, "mail-penalty-expired");
                    },
                    mailComp.PriorityCancelToken.Token);
            }

            _appearanceSystem.SetData(uid, MailVisuals.JobIcon, recipient.JobIcon);

            _metaDataSystem.SetEntityName(uid,
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
            var receiverStation = _stationSystem.GetOwningStation(receiverUid);

            while (query.MoveNext(out var uid, out var mailTeleporter))
            {
                var teleporterStation = _stationSystem.GetOwningStation(uid);
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
            if (_idCardSystem.TryFindIdCard(receiverUid, out var idCard)
                && TryComp<AccessComponent>(idCard.Owner, out var access)
                && idCard.Comp.FullName != null)
            {
                var accessTags = access.Tags;
                var mayReceivePriorityMail = !(_mindSystem.GetMind(receiverUid) == null);

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
            var teleporterStation = _stationSystem.GetOwningStation(uid);

            while (query.MoveNext(out var receiverUid, out _))
            {
                var receiverStation = _stationSystem.GetOwningStation(receiverUid);
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
        private void SpawnMail(EntityUid uid, MailTeleporterComponent? component = null)
        {
            if (!Resolve(uid, ref component))
            {
                _sawmill.Error($"Tried to SpawnMail on {ToPrettyString(uid)} without a valid MailTeleporterComponent!");
                return;
            }

            if (GetUndeliveredParcelCount(uid) >= component.MaximumUndeliveredParcels)
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

            var deliveryCount = component.MinimumDeliveriesPerTeleport + candidateList.Count / component.CandidatesPerDelivery;

            for (var i = 0; i < deliveryCount; i++)
            {
                var candidate = _random.Pick(candidateList);
                var possibleParcels = new Dictionary<string, float>(pool.Everyone);

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

                _tagSystem.AddTag(mail, "Mail"); // Frontier
            }

            if (_containerSystem.TryGetContainer(uid, "queued", out var queued))
                _containerSystem.EmptyContainer(queued);

            _audioSystem.PlayPvs(component.TeleportSound, uid);
        }

        private void OpenMail(EntityUid uid, MailComponent? component = null, EntityUid? user = null)
        {
            if (!Resolve(uid, ref component))
                return;

            _audioSystem.PlayPvs(component.OpenSound, uid);

            if (user != null)
                _handsSystem.TryDrop((EntityUid) user);

            if (!_containerSystem.TryGetContainer(uid, "contents", out var contents))
            {
                // I silenced this error because it fails non deterministically in tests and doesn't seem to effect anything else.
                // _sawmill.Error($"Mail {ToPrettyString(uid)} was missing contents container!");
                return;
            }

            foreach (var entity in contents.ContainedEntities.ToArray())
            {
                _handsSystem.PickupOrDrop(user, entity);
            }

            _tagSystem.AddTag(uid, "Trash");
            _tagSystem.AddTag(uid, "Recyclable");
            component.IsEnabled = false;
            UpdateMailTrashState(uid, true);
        }

        private void UpdateAntiTamperVisuals(EntityUid uid, bool isLocked)
        {
            _appearanceSystem.SetData(uid, MailVisuals.IsLocked, isLocked);
        }

        private void UpdateMailTrashState(EntityUid uid, bool isTrash)
        {
            _appearanceSystem.SetData(uid, MailVisuals.IsTrash, isTrash);
        }

        // DeltaV - Helper function that executes for each StationLogisticsStatsComponent
        // For updating MailMetrics stats
        private void ExecuteForEachLogisticsStats(EntityUid uid,
            Action<EntityUid, StationLogisticStatsComponent> action)
        {

            var query = EntityQueryEnumerator<StationLogisticStatsComponent>();
            while (query.MoveNext(out var station, out var logisticStats))
            {
                if (_stationSystem.GetOwningStation(uid) != station)
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
}
