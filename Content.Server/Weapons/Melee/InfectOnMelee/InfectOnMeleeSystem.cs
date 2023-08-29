using Content.Server.Administration.Commands;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Shared.Mobs;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Stunnable;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Server.Emoting.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Cluwne;
using Content.Shared.Interaction.Components;
using Content.Server.Abilities.Mime;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Zombies;
using Content.Server.NPC.Systems;
using Content.Shared.Weapons.Melee;

namespace Content.Server.Weapons.Melee.InfectOnMelee;

public sealed class InfectOnMeleeSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InfectOnMeleeComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(EntityUid uid, InfectOnMeleeComponent component, MeleeHitEvent args)
    {
        if (component.Cluwinification == true)
        {
            foreach (var entity in args.HitEntities)
            {
                if (HasComp<HumanoidAppearanceComponent>(entity)
                && !_mobStateSystem.IsDead(entity)
                && _robustRandom.Prob(component.InfectionChance)
                && !HasComp<ClumsyComponent>(entity)
                && !HasComp<ZombieComponent>(entity))
                {
                    _audio.PlayPvs(component.InfectionSound, uid);
                    EnsureComp<CluwneComponent>(entity);
                }
            }
        }
    }
}
