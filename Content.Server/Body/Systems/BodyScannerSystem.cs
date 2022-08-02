using Content.Server.Body.Components;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Server.GameObjects;
using System.Linq;

namespace Content.Server.Body.Systems
{
    public sealed class BodyScannerSystem : EntitySystem
    {
        [Dependency] private readonly BodyPartSystem _bodyPartSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BodyScannerComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
        }

        private void OnBoundUIOpened(EntityUid uid, BodyScannerComponent component, BoundUIOpenedEvent args)
        {
            var sessionEnt = args.Session.AttachedEntity;
            if (sessionEnt == null || !TryComp<BodyComponent>(sessionEnt, out var body))
                return;

            UpdateBodyScannerUiState(component, body);
        }

        private void UpdateBodyScannerUiState(BodyScannerComponent component, BodyComponent body)
        {
            var bodyPartStates = new Dictionary<string, BodyPartUiState>();

            foreach (var (slotId, slot) in body.Slots)
            {
                if (!slot.HasPart)
                    continue;

                if (!TryComp<SharedBodyPartComponent>(slot.Part, out var part))
                    continue;

                var partName = Comp<MetaDataComponent>(part.Owner).EntityName;

                FixedPoint2 damage = 0;
                if (TryComp<DamageableComponent>(part.Owner, out var damageable))
                    damage = damageable.TotalDamage;

                var mechList = new List<string>();
                foreach (var mechanism in _bodyPartSystem.GetAllMechanisms(part.Owner, part))
                {
                    mechList.Add(Comp<MetaDataComponent>(mechanism.Owner).EntityName);
                }

                bodyPartStates.Add(slotId, new BodyPartUiState(partName, damage, mechList));
            }

            if (!bodyPartStates.Any())
                return;

            var state = new BodyScannerUIState(bodyPartStates);
            _uiSystem.GetUiOrNull(component.Owner, BodyScannerUiKey.Key)?.SetState(state);
        }
    }
}
