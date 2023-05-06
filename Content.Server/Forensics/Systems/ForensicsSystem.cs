using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Robust.Shared.Random;

namespace Content.Server.Forensics
{
    public sealed class ForensicsSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        public override void Initialize()
        {
            SubscribeLocalEvent<FingerprintComponent, ContactInteractionEvent>(OnInteract);
            SubscribeLocalEvent<FingerprintComponent, ComponentInit>(OnFingeprintInit);
            SubscribeLocalEvent<DnaComponent, ComponentInit>(OnDNAInit);
        }

        private void OnInteract(EntityUid uid, FingerprintComponent component, ContactInteractionEvent args)
        {
            ApplyEvidence(uid, args.Other);
        }

        private void OnFingeprintInit(EntityUid uid, FingerprintComponent component, ComponentInit args)
        {
            component.Fingerprint = GenerateFingerprint();
        }

        private void OnDNAInit(EntityUid uid, DnaComponent component, ComponentInit args)
        {
            component.DNA = GenerateDNA();
        }

        private string GenerateFingerprint()
        {
            byte[] fingerprint = new byte[16];
            _random.NextBytes(fingerprint);
            return Convert.ToHexString(fingerprint);
        }

        private string GenerateDNA()
        {
            var letters = new List<string> { "A", "C", "G", "T" };
            string DNA = String.Empty;

            for (int i = 0; i < 16; i++)
            {
                DNA += letters[_random.Next(letters.Count)];
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
    }
}
