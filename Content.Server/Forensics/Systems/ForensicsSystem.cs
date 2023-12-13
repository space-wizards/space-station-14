using Content.Server.Body.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.DoAfter;
using Content.Shared.DoAfter;
using Content.Shared.Forensics;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;

namespace Content.Server.Forensics
{
    public sealed class ForensicsSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        public override void Initialize()
        {
            SubscribeLocalEvent<FingerprintComponent, ContactInteractionEvent>(OnInteract);
            SubscribeLocalEvent<FingerprintComponent, MapInitEvent>(OnFingerprintInit);
            SubscribeLocalEvent<DnaComponent, MapInitEvent>(OnDNAInit);

            SubscribeLocalEvent<DnaComponent, BeingGibbedEvent>(OnBeingGibbed);
            SubscribeLocalEvent<ForensicsComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<ForensicsComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<ForensicsComponent, CleanForensicsDoAfterEvent>(OnCleanForensicsDoAfter);
            SubscribeLocalEvent<DnaComponent, TransferDnaEvent>(OnTransferDnaEvent);
        }

        private void OnInteract(EntityUid uid, FingerprintComponent component, ContactInteractionEvent args)
        {
            ApplyEvidence(uid, args.Other);
        }

        private void OnFingerprintInit(EntityUid uid, FingerprintComponent component, MapInitEvent args)
        {
            component.Fingerprint = GenerateFingerprint();
        }

        private void OnDNAInit(EntityUid uid, DnaComponent component, MapInitEvent args)
        {
            component.DNA = GenerateDNA();
        }

        private void OnBeingGibbed(EntityUid uid, DnaComponent component, BeingGibbedEvent args)
        {
            foreach(EntityUid part in args.GibbedParts)
            {
                var partComp = EnsureComp<ForensicsComponent>(part);
                partComp.DNAs.Add(component.DNA);
                partComp.CanDnaBeCleaned = false;
            }
        }

        private void OnMeleeHit(EntityUid uid, ForensicsComponent component, MeleeHitEvent args)
        {
            if((args.BaseDamage.DamageDict.TryGetValue("Blunt", out var bluntDamage) && bluntDamage.Value > 0) ||
                (args.BaseDamage.DamageDict.TryGetValue("Slash", out var slashDamage) && slashDamage.Value > 0) ||
                (args.BaseDamage.DamageDict.TryGetValue("Piercing", out var pierceDamage) && pierceDamage.Value > 0))
            {
                foreach(EntityUid hitEntity in args.HitEntities)
                {
                    if(TryComp<DnaComponent>(hitEntity, out var hitEntityComp))
                        component.DNAs.Add(hitEntityComp.DNA);
                }
            }
        }

        private void OnAfterInteract(EntityUid uid, ForensicsComponent component, AfterInteractEvent args)
        {
            if (args.Handled)
                return;

            if (!_tagSystem.HasTag(args.Used, "CleansForensics"))
                return;

            if((component.DNAs.Count > 0 && component.CanDnaBeCleaned) || (component.Fingerprints.Count + component.Fibers.Count > 0))
            {
                var doAfterArgs = new DoAfterArgs(EntityManager, args.User, component.CleanDelay, new CleanForensicsDoAfterEvent(), uid, target: args.Target, used: args.Used)
                {
                    BreakOnHandChange = true,
                    NeedHand = true,
                    BreakOnDamage = true,
                    BreakOnTargetMove = true,
                    MovementThreshold = 0.01f,
                    DistanceThreshold = component.CleanDistance,
                };


                _doAfterSystem.TryStartDoAfter(doAfterArgs);

                args.Handled = true;
            }
        }

        private void OnCleanForensicsDoAfter(EntityUid uid, ForensicsComponent component, CleanForensicsDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || args.Args.Target == null)
                return;

            if (!TryComp<ForensicsComponent>(args.Target, out var targetComp))
                return;

            targetComp.Fibers = new();
            targetComp.Fingerprints = new();

            if (targetComp.CanDnaBeCleaned)
                targetComp.DNAs = new();

            // leave behind evidence it was cleaned
            if (TryComp<FiberComponent>(args.Used, out var fiber))
                targetComp.Fibers.Add(string.IsNullOrEmpty(fiber.FiberColor) ? Loc.GetString("forensic-fibers", ("material", fiber.FiberMaterial)) : Loc.GetString("forensic-fibers-colored", ("color", fiber.FiberColor), ("material", fiber.FiberMaterial)));

            if (TryComp<ResidueComponent>(args.Used, out var residue))
                targetComp.Residues.Add(string.IsNullOrEmpty(residue.ResidueColor) ? Loc.GetString("forensic-residue", ("adjective", residue.ResidueAdjective)) : Loc.GetString("forensic-residue-colored", ("color", residue.ResidueColor), ("adjective", residue.ResidueAdjective)));
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
            var component = EnsureComp<ForensicsComponent>(target);
            if (_inventory.TryGetSlotEntity(user, "gloves", out var gloves))
            {
                if (TryComp<FiberComponent>(gloves, out var fiber) && !string.IsNullOrEmpty(fiber.FiberMaterial))
                    component.Fibers.Add(string.IsNullOrEmpty(fiber.FiberColor) ? Loc.GetString("forensic-fibers", ("material", fiber.FiberMaterial)) : Loc.GetString("forensic-fibers-colored", ("color", fiber.FiberColor), ("material", fiber.FiberMaterial)));

                if (HasComp<FingerprintMaskComponent>(gloves))
                    return;
            }
            if (TryComp<FingerprintComponent>(user, out var fingerprint))
                component.Fingerprints.Add(fingerprint.Fingerprint ?? "");
        }

        private void OnTransferDnaEvent(EntityUid uid, DnaComponent component, ref TransferDnaEvent args)
        {
            var recipientComp = EnsureComp<ForensicsComponent>(args.Recipient);
            recipientComp.DNAs.Add(component.DNA);
            recipientComp.CanDnaBeCleaned = args.CanDnaBeCleaned;
        }

        #region Public API

        /// <summary>
        /// Transfer DNA from one entity onto the forensics of another
        /// </summary>
        /// <param name="recipient">The entity receiving the DNA</param>
        /// <param name="donor">The entity applying its DNA</param>
        /// <param name="canDnaBeCleaned">If this DNA be cleaned off of the recipient. e.g. cleaning a knife vs cleaning a puddle of blood</param>
        public void TransferDna(EntityUid recipient, EntityUid donor, bool canDnaBeCleaned = true)
        {
            if (TryComp<DnaComponent>(donor, out var donorComp))
            {
                EnsureComp<ForensicsComponent>(recipient, out var recipientComp);
                recipientComp.DNAs.Add(donorComp.DNA);
            }
        }

        #endregion
    }
}
