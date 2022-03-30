using System.Linq;
using System.Text;
using Content.Server.Hands.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server.Forensics
{
    public sealed class ForensicsSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly HandsSystem _handsSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<ForensicsComponent, InteractHandEvent>((uid, component, args) => ApplyEvidence(args.User, component));
            SubscribeLocalEvent<ForensicsComponent, GettingInteractedWithAttemptEvent>((uid, component, args) =>
            {
                Hand hand = _handsSystem.EnumerateHands(args.Uid).First();
                if (HasComp<ForensicScannerComponent>(hand.HeldEntity))
                    return;
                ApplyEvidence(args.Uid, component);
            });
            SubscribeLocalEvent<ForensicsComponent, ContainerGettingInsertedAttemptEvent>((uid, component, args) => ApplyEvidence(args.EntityUid, component));
            SubscribeLocalEvent<ForensicsComponent, GettingPickedUpAttemptEvent>((uid, component, args) => ApplyEvidence(args.User, component));
            SubscribeLocalEvent<FingerprintComponent, ComponentStartup>(OnFingerprintStartup);
        }

        private void OnFingerprintStartup(EntityUid uid, FingerprintComponent component, ComponentStartup args)
        {
            component.Fingerprint = GenerateFingerprint();
        }

        private string GenerateFingerprint()
        {
            byte[] fingerprint = new byte[16];
            _random.NextBytes(fingerprint);
            return Convert.ToHexString(fingerprint);
        }

        private void ApplyEvidence(EntityUid user, ForensicsComponent component)
        {
            if (_inventory.TryGetSlotEntity(user, "gloves", out var gloves))
            {
                if (TryComp<FiberComponent>(gloves, out var fiber) && fiber.FiberDescription != null)
                {
                    component.Fibers.Add(fiber.FiberDescription);
                }
                if (!TryComp<FingerprintMaskComponent>(gloves, out var fingerprintMask))
                {
                    if (TryComp<FingerprintComponent>(user, out var fingerprint))
                    {
                        component.Fingerprints.Add(fingerprint.Fingerprint ?? "");
                    }
                }
            }
            else
            {
                if (TryComp<FingerprintComponent>(user, out var fingerprint))
                {
                    component.Fingerprints.Add(fingerprint.Fingerprint ?? "");
                }
            }
        }
    }
}
