using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Server.Destructible.Events;
using Robust.Shared.Random;

namespace Content.Server.Forensics
{
    public sealed class ForensicsSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FingerprintComponent, ContactInteractionEvent>(OnInteract);
            SubscribeLocalEvent<FingerprintComponent, MapInitEvent>(OnFingerprintInit);
            SubscribeLocalEvent<DnaComponent, MapInitEvent>(OnDNAInit);

            SubscribeLocalEvent<ForensicsComponent, DestructionSpawnBehavior>(OnDestructionSpawnBehavior);
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

        private string GenerateFingerprint()
        {
            var fingerprint = new byte[16];
            _random.NextBytes(fingerprint);
            return Convert.ToHexString(fingerprint);
        }

        private string GenerateDNA()
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

        private void OnDestructionSpawnBehavior(EntityUid uid, ForensicsComponent component, ref DestructionSpawnBehavior ev)
        {
            var transferDNA = _random.Prob(component.DNATransferChanceAfterDestroy);
            var transferRestOf = _random.Prob(component.RestOfTransferChanceAfterDestroy);

            // Ensure component only if something should be transfered
            if (transferDNA || transferRestOf)
            {
                var spawnedForensics = EnsureComp<ForensicsComponent>(ev.Spawned);
                if (transferDNA)
                    spawnedForensics.DNAs = component.DNAs;
                
                if (transferRestOf)
                {
                    spawnedForensics.Fingerprints = component.Fingerprints;
                    spawnedForensics.Fibers = component.Fibers;
                }
            }
        }

    }
}
