using Content.Shared.Inventory;
using Content.Shared.Item;
using Robust.Shared.Random;

namespace Content.Server.Forensics
{
    public sealed class ForensicsSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        public override void Initialize()
        {
            SubscribeLocalEvent<FingerprintComponent, UserInteractedWithItemEvent>(OnInteract);
            SubscribeLocalEvent<FingerprintComponent, ComponentInit>(OnInit);
        }

        private void OnInteract(EntityUid uid, FingerprintComponent component, UserInteractedWithItemEvent args)
        {
            ApplyEvidence(args.User, args.Item);
        }

        private void OnInit(EntityUid uid, FingerprintComponent component, ComponentInit args)
        {
            component.Fingerprint = GenerateFingerprint();
        }

        private string GenerateFingerprint()
        {
            byte[] fingerprint = new byte[16];
            _random.NextBytes(fingerprint);
            return Convert.ToHexString(fingerprint);
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
