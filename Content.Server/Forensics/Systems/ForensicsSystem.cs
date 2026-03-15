using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Forensics.Components;
using Content.Server.Popups;
using Content.Shared.Body.Events;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.DoAfter;
using Content.Shared.Forensics;
using Content.Shared.Forensics.Components;
using Content.Shared.Forensics.Systems;
using Content.Shared.Gibbing;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Shared.Hands.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Forensics
{
    public sealed class ForensicsSystem : SharedForensicsSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        private static readonly ProtoId<ForensicEvidencePrototype> DNAEvidence = "dna";
        private static readonly ProtoId<ForensicEvidencePrototype> FingerprintsEvidence = "fingerprints";
        private static readonly ProtoId<ForensicEvidencePrototype> FibersEvidence = "fibers";

        public override void Initialize()
        {
            SubscribeLocalEvent<HandsComponent, ContactInteractionEvent>(OnInteract);
            SubscribeLocalEvent<FingerprintComponent, MapInitEvent>(OnFingerprintInit, after: new[] { typeof(BloodstreamSystem) });
            // The solution entities are spawned on MapInit as well, so we have to wait for that to be able to set the DNA in the bloodstream correctly without ResolveSolution failing
            SubscribeLocalEvent<DnaComponent, MapInitEvent>(OnDNAInit, after: new[] { typeof(BloodstreamSystem) });

            SubscribeLocalEvent<ForensicsComponent, GibbedBeforeDeletionEvent>(OnBeingGibbed);
            SubscribeLocalEvent<ForensicsComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<ForensicsComponent, GotRehydratedEvent>(OnRehydrated);
            SubscribeLocalEvent<CleansForensicsComponent, AfterInteractEvent>(OnAfterInteract, after: new[] { typeof(AbsorbentSystem) });
            SubscribeLocalEvent<ForensicsComponent, CleanForensicsDoAfterEvent>(OnCleanForensicsDoAfter);
            SubscribeLocalEvent<DnaComponent, TransferDnaEvent>(OnTransferDnaEvent);
            SubscribeLocalEvent<DnaSubstanceTraceComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
            SubscribeLocalEvent<CleansForensicsComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
        }

        /// <summary>
        /// Adds an evidence of the specified type to the forensics component of the entity.
        /// If no component exists for that entity, one will be added.
        /// </summary>
        /// <param name="ent">Entity to add the evidence to.</param>
        /// <param name="type">ProtoID for the forensics type to add.</param>
        /// <param name="evidence">Evidence to add.</param>
        /// <param name="comp">Optional ForensicsComponent if one already exists.</param>
        public void AddEvidence(EntityUid ent,
            ProtoId<ForensicEvidencePrototype> type,
            string evidence,
            ForensicsComponent? comp = null)
        {
            comp ??= EnsureComp<ForensicsComponent>(ent);
            comp.Evidence.GetOrNew(type).Add(evidence);
        }

        /// <summary>
        /// Adds an evidence of the specified type to the forensics component of the entity.
        /// If no component exists for that entity, one will be added.
        /// </summary>
        /// <param name="ent">Entity to add the evidence to.</param>
        /// <param name="type">ProtoID for the forensics type to add.</param>
        /// <param name="evidence">Hashset of evidence to add.</param>
        /// <param name="comp">Optional ForensicsComponent if one already exists.</param>
        public void AddEvidence(EntityUid ent,
            ProtoId<ForensicEvidencePrototype> type,
            HashSet<string> evidence,
            ForensicsComponent? comp = null)
        {
            comp ??= EnsureComp<ForensicsComponent>(ent);
            comp.Evidence.GetOrNew(type).UnionWith(evidence);
        }

        private void OnSolutionChanged(Entity<DnaSubstanceTraceComponent> ent, ref SolutionContainerChangedEvent ev)
        {
            var soln = GetSolutionsDNA(ev.Solution);
            if (soln.Count > 0)
            {
                AddEvidence(ent, DNAEvidence, [.. soln]);
            }
        }

        private void OnInteract(EntityUid uid, HandsComponent component, ContactInteractionEvent args)
        {
            ApplyEvidence(uid, args.Other);
        }

        private void OnFingerprintInit(Entity<FingerprintComponent> ent, ref MapInitEvent args)
        {
            if (ent.Comp.Fingerprint == null)
                RandomizeFingerprint((ent.Owner, ent.Comp));
        }

        private void OnDNAInit(Entity<DnaComponent> ent, ref MapInitEvent args)
        {
            if (ent.Comp.DNA == null)
                RandomizeDNA((ent.Owner, ent.Comp));
            else
            {
                // If set manually (for example by cloning) we also need to inform the bloodstream of the correct DNA string so it can be updated
                var ev = new GenerateDnaEvent { Owner = ent.Owner, DNA = ent.Comp.DNA };
                RaiseLocalEvent(ent.Owner, ref ev);
            }
        }

        private void OnBeingGibbed(Entity<ForensicsComponent> ent, ref GibbedBeforeDeletionEvent args)
        {
            string dna = Loc.GetString("forensics-dna-unknown");

            if (TryComp(ent, out DnaComponent? dnaComp) && dnaComp.DNA != null)
                dna = dnaComp.DNA;

            foreach (var part in args.Giblets)
            {
                var partComp = EnsureComp<ForensicsComponent>(part);
                AddEvidence(part, DNAEvidence, dna, partComp);
                partComp.CleanBlacklist.Add(DNAEvidence);
            }
        }

        private void OnMeleeHit(EntityUid uid, ForensicsComponent component, MeleeHitEvent args)
        {
            if ((args.BaseDamage.DamageDict.TryGetValue("Blunt", out var bluntDamage) && bluntDamage.Value > 0) ||
                (args.BaseDamage.DamageDict.TryGetValue("Slash", out var slashDamage) && slashDamage.Value > 0) ||
                (args.BaseDamage.DamageDict.TryGetValue("Piercing", out var pierceDamage) && pierceDamage.Value > 0))
            {
                foreach (EntityUid hitEntity in args.HitEntities)
                {
                    if (TryComp<DnaComponent>(hitEntity, out var hitEntityComp) && hitEntityComp.DNA != null)
                        AddEvidence(uid, DNAEvidence, hitEntityComp.DNA, component);
                }
            }
        }

        private void OnRehydrated(Entity<ForensicsComponent> ent, ref GotRehydratedEvent args)
        {
            CopyForensicsFrom(ent.Comp, args.Target);
        }

        /// <summary>
        /// Copy forensic information from a source entity to a destination.
        /// Existing forensic information on the target is still kept.
        /// </summary>
        public void CopyForensicsFrom(ForensicsComponent src, EntityUid target)
        {
            var dest = EnsureComp<ForensicsComponent>(target);
            dest.Evidence = src.Evidence;
        }

        public List<string> GetSolutionsDNA(EntityUid uid)
        {
            List<string> list = new();
            if (TryComp<SolutionContainerManagerComponent>(uid, out var comp))
            {
                foreach (var (_, soln) in _solutionContainerSystem.EnumerateSolutions((uid, comp)))
                {
                    list.AddRange(GetSolutionsDNA(soln.Comp.Solution));
                }
            }
            return list;
        }

        public List<string> GetSolutionsDNA(Solution soln)
        {
            List<string> list = new();
            foreach (var reagent in soln.Contents)
            {
                foreach (var data in reagent.Reagent.EnsureReagentData())
                {
                    if (data is DnaData)
                    {
                        list.Add(((DnaData) data).DNA);
                    }
                }
            }
            return list;
        }
        private void OnAfterInteract(Entity<CleansForensicsComponent> cleanForensicsEntity, ref AfterInteractEvent args)
        {
            if (args.Handled || !args.CanReach || args.Target == null)
                return;

            args.Handled = TryStartCleaning(cleanForensicsEntity, args.User, args.Target.Value);
        }

        private void OnUtilityVerb(Entity<CleansForensicsComponent> entity, ref GetVerbsEvent<UtilityVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            // These need to be set outside for the anonymous method!
            var user = args.User;
            var target = args.Target;

            var verb = new UtilityVerb()
            {
                Act = () => TryStartCleaning(entity, user, target),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/bubbles.svg.192dpi.png")),
                Text = Loc.GetString("forensics-verb-text"),
                Message = Loc.GetString("forensics-verb-message"),
                // This is important because if its true using the cleaning device will count as touching the object.
                DoContactInteraction = false
            };

            args.Verbs.Add(verb);
        }

        /// <summary>
        ///     Attempts to clean the given item with the given CleansForensics entity.
        /// </summary>
        /// <param name="cleanForensicsEntity">The entity that is being used to clean the target.</param>
        /// <param name="user">The user that is using the cleanForensicsEntity.</param>
        /// <param name="target">The target of the forensics clean.</param>
        /// <returns>True if the target can be cleaned and has some sort of DNA or fingerprints / fibers and false otherwise.</returns>
        public bool TryStartCleaning(Entity<CleansForensicsComponent> cleanForensicsEntity, EntityUid user, EntityUid target)
        {
            if (!TryComp<ForensicsComponent>(target, out var forensicsComp))
            {
                _popupSystem.PopupEntity(Loc.GetString("forensics-cleaning-cannot-clean", ("target", Identity.Entity(target, EntityManager))), user, user, PopupType.MediumCaution);
                return false;
            }

            List<ProtoId<ForensicEvidencePrototype>> toClean = [];
            foreach (var protoId in forensicsComp.Evidence.Keys)
            {
                var proto = _prototypeManager.Index(protoId);
                if (!proto.Cleanable ||
                    forensicsComp.CleanBlacklist.Contains(proto) ||
                    cleanForensicsEntity.Comp.Blacklist.Contains(proto))
                    continue; // This evidence is not cleanable

                toClean.Add(protoId);
            }

            if (toClean.Count > 0)
            {
                var cleanDelay = cleanForensicsEntity.Comp.CleanDelay;
                var doAfterArgs = new DoAfterArgs(
                    EntityManager,
                    user,
                    cleanDelay,
                    new CleanForensicsDoAfterEvent
                    {
                        ToClean = toClean
                    },
                    cleanForensicsEntity,
                    target: target,
                    used: cleanForensicsEntity)
                {
                    NeedHand = true,
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    MovementThreshold = 0.01f,
                    DistanceThreshold = forensicsComp.CleanDistance,
                };

                _doAfterSystem.TryStartDoAfter(doAfterArgs);

                _popupSystem.PopupEntity(Loc.GetString("forensics-cleaning", ("target", Identity.Entity(target, EntityManager))), user, user);

                return true;
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString("forensics-cleaning-cannot-clean", ("target", Identity.Entity(target, EntityManager))), user, user, PopupType.MediumCaution);
                return false;
            }

        }

        private void OnCleanForensicsDoAfter(EntityUid uid, ForensicsComponent component, CleanForensicsDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || args.Args.Target == null)
                return;

            if (!TryComp<ForensicsComponent>(args.Target, out var targetComp))
                return;

            foreach (var key in args.ToClean)
            {
                // Evidence to clean has already been validated at the DoAfter call site
                targetComp.Evidence.Remove(key);
            }

            // leave behind evidence it was cleaned
            if (TryComp<FiberComponent>(args.Used, out var fiber))
                AddEvidence(args.Target.Value,
                    FibersEvidence,
                    string.IsNullOrEmpty(fiber.FiberColor)
                        ? Loc.GetString("forensic-fibers", ("material", fiber.FiberMaterial))
                        : Loc.GetString("forensic-fibers-colored",
                            ("color", fiber.FiberColor),
                            ("material", fiber.FiberMaterial)),
                    targetComp);

            if (TryComp<CleansForensicsComponent>(args.Used, out var agent))
                targetComp.CleaningAgents.Add(string.IsNullOrEmpty(agent.AgentColor)
                    ? Loc.GetString("forensic-cleaning-agent", ("adjective", agent.AgentAdjective))
                    : Loc.GetString("forensic-cleaning-agent-colored",
                        ("color", agent.AgentColor),
                        ("adjective", agent.AgentAdjective)));
        }

        public string GenerateFingerprint()
        {
            var fingerprint = new byte[16];
            _random.NextBytes(fingerprint);
            return Convert.ToHexString(fingerprint);
        }

        public string GenerateDNA()
        {
            var letters = new[] { "A", "C", "G", "T" };
            var DNA = string.Empty;

            for (var i = 0; i < 16; i++)
            {
                DNA += letters[_random.Next(letters.Length)];
            }

            return DNA;
        }

        private void ApplyEvidence(EntityUid user, EntityUid target)
        {
            if (HasComp<IgnoresFingerprintsComponent>(target))
                return;

            var component = EnsureComp<ForensicsComponent>(target);
            if (_inventory.TryGetSlotEntity(user, "gloves", out var gloves))
            {
                if (TryComp<FiberComponent>(gloves, out var fiber) && !string.IsNullOrEmpty(fiber.FiberMaterial))
                    AddEvidence(user,
                        FibersEvidence,
                        string.IsNullOrEmpty(fiber.FiberColor)
                            ? Loc.GetString("forensic-fibers", ("material", fiber.FiberMaterial))
                            : Loc.GetString("forensic-fibers-colored",
                                ("color", fiber.FiberColor),
                                ("material", fiber.FiberMaterial)),
                        component);
            }

            if (TryComp<FingerprintComponent>(user, out var fingerprint) && CanAccessFingerprint(user, out _))
                AddEvidence(user, FingerprintsEvidence, fingerprint.Fingerprint ?? "", component);
        }

        // TODO: Delete this. A lot of systems are manually raising this method event instead of calling the identical <see cref="TransferDna"/> method.
        // According to our code conventions we should not use method events.
        private void OnTransferDnaEvent(EntityUid uid, DnaComponent component, ref TransferDnaEvent args)
        {
            if (component.DNA == null)
                return;

            var recipientComp = EnsureComp<ForensicsComponent>(args.Recipient);
            AddEvidence(args.Recipient, DNAEvidence, component.DNA, recipientComp);
            if (args.CanDnaBeCleaned)
                recipientComp.CleanBlacklist.Remove(DNAEvidence);
            else
                recipientComp.CleanBlacklist.Add(DNAEvidence);

        }

        #region Public API
        public override void RandomizeDNA(Entity<DnaComponent?> ent)
        {
            if (!Resolve(ent, ref ent.Comp, false))
                return;

            ent.Comp.DNA = GenerateDNA();
            Dirty(ent);

            var ev = new GenerateDnaEvent { Owner = ent.Owner, DNA = ent.Comp.DNA };
            RaiseLocalEvent(ent.Owner, ref ev);
        }

        public override void RandomizeFingerprint(Entity<FingerprintComponent?> ent)
        {
            if (!Resolve(ent, ref ent.Comp, false))
                return;

            ent.Comp.Fingerprint = GenerateFingerprint();
            Dirty(ent);
        }

        public override void TransferDna(EntityUid recipient, EntityUid donor, bool canDnaBeCleaned = true)
        {
            if (TryComp<DnaComponent>(donor, out var donorComp) && donorComp.DNA != null)
            {
                EnsureComp<ForensicsComponent>(recipient, out var recipientComp);
                AddEvidence(recipient, DNAEvidence, donorComp.DNA, recipientComp);
                if (!canDnaBeCleaned)
                    recipientComp.CleanBlacklist.Add(DNAEvidence);
            }
        }

        /// <summary>
        /// Checks if there's a way to access the fingerprint of the target entity.
        /// </summary>
        /// <param name="target">The entity with the fingerprint</param>
        /// <param name="blocker">The entity that blocked accessing the fingerprint</param>
        public bool CanAccessFingerprint(EntityUid target, out EntityUid? blocker)
        {
            var ev = new TryAccessFingerprintEvent();

            RaiseLocalEvent(target, ev);
            if (!ev.Cancelled && TryComp<InventoryComponent>(target, out var inv))
                _inventory.RelayEvent((target, inv), ev);

            blocker = ev.Blocker;
            return !ev.Cancelled;
        }

        #endregion
    }
}
