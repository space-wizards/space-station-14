using Robust.Shared.Player;
using Content.Server.Speech.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Disease.Components;
using Content.Server.Disease.Zombie.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Popups;
using Content.Server.Atmos.Components;
using Content.Server.Hands.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Mind.Components;
using Content.Server.Chat.Managers;
using Content.Server.Inventory;
using Content.Shared.Damage;
using Content.Shared.MobState.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.CharacterAppearance.Systems;
using Content.Server.Weapons.Melee.ZombieTransfer.Components;
using Content.Server.Polymorph.Systems;

namespace Content.Server.Zombies
{
    /// <summary>
    /// Handles zombie propagation and inherent zombie traits
    /// </summary>
    public sealed class ZombifyOnDeathSystem : EntitySystem
    {
        [Dependency] private readonly PolymorphableSystem _polymorph = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ZombifyOnDeathComponent, DamageChangedEvent>(OnDamageChanged);
        }    

        private void OnDamageChanged(EntityUid uid, ZombifyOnDeathComponent component, DamageChangedEvent args)
        {
            if (!TryComp<MobStateComponent>(uid, out var mobstate))
                return;

            if (mobstate.IsDead()||
                mobstate.IsCritical())
            {
                var zombieUid = _polymorph.PolymorphEntity(uid, "ZombieGeneric");
            }
        }
    }
}
