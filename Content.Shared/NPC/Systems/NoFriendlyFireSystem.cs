using Content.Shared.NPC.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.NPC.Systems;

public sealed class NoFriendlyFireSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _faction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NoFriendlyFireComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<NoFriendlyFireComponent> ent, ref MeleeHitEvent args)
    {
        if (!TryComp<NpcFactionMemberComponent>(ent, out var faction))
            return;

        var factionEnt = (ent.Owner, faction);
        foreach (var hit in args.HitEntities)
        {
            if (_faction.IsEntityFriendly(factionEnt, hit))
            {
                args.BonusDamage = -args.BaseDamage;
                return; // prevent healing by swinging multiple friendlies or something
            }
        }
    }
}
