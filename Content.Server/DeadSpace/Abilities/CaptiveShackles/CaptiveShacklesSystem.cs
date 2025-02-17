using Content.Server.DeadSpace.Abilities.CaptiveShackles.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Abilities.CaptiveShackles
{
    public sealed class CaptiveShacklesSystem : EntitySystem
    {
        [Dependency] private readonly NpcFactionSystem _faction = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CaptiveShacklesComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<CaptiveShacklesComponent, GotUnequippedEvent>(OnUnequipped);
        }

        private void OnEquipped(EntityUid uid, CaptiveShacklesComponent comp, GotEquippedEvent args)
        {
            if (TryComp<NpcFactionMemberComponent>(args.Equipee, out var factionComp))
                comp.OldFaction = GetFirstElement(factionComp.Factions);

            _faction.ClearFactions(args.Equipee, dirty: false);
            _faction.AddFaction(args.Equipee, "SimpleNeutral");
        }

        private void OnUnequipped(EntityUid uid, CaptiveShacklesComponent comp, GotUnequippedEvent args)
        {
            _faction.ClearFactions(args.Equipee, dirty: false);

            if (comp.OldFaction != null)
                _faction.AddFaction(args.Equipee, comp.OldFaction);

        }

        static ProtoId<NpcFactionPrototype>? GetFirstElement(HashSet<ProtoId<NpcFactionPrototype>> set)
        {
            foreach (var element in set)
            {
                return element;
            }

            return null;
        }
    }
}
