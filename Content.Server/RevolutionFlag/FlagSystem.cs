using Content.Server.RevolutionFlag.Components;
using Content.Server.Mind.Components;
using Content.Server.Traitor;
using Content.Shared.Damage.Events;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Damage;
using Content.Shared.Hands;
using Content.Shared.StatusEffect;

namespace Content.Server.RevolutionFlag;
    public sealed class FlagSystem : EntitySystem
    {
        [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffectSystem = default!;
        private const string RevolutionaryPrototypeId = "Revolutionary";
        private const string CourageEffectKey = "Courage";
        private DamageModifierSet Modifiers = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FlagBuffComponent,MeleeHitEvent>(ModifyDamage);
            SubscribeLocalEvent<FlagComponent,GotEquippedHandEvent>(OnEquipped);
            SubscribeLocalEvent<FlagComponent,GotUnequippedHandEvent>(OnDequipped);
        }

        private void OnEquipped(EntityUid uid, FlagComponent comp, GotEquippedHandEvent args)
        {
            comp.active = true;
        }

        private void OnDequipped(EntityUid uid, FlagComponent comp, GotUnequippedHandEvent args)
        {
            comp.active = false;
        }

        public void Aura(FlagComponent flagComp, float range)
        {
            var transform = EntityManager.GetComponent<TransformComponent>(flagComp.Owner);
            var mapPosition = transform.MapPosition;
            var inRange = _entityLookup.GetEntitiesInRange(transform.Coordinates, range);

            if (!flagComp.active)
                return;
            foreach(var entityInRange in inRange)
            {
                if (!TryComp<MindComponent>(entityInRange, out var mind))
                    continue;
                if (!GetRole(mind, RevolutionaryPrototypeId))
                    continue;
                _statusEffectSystem.TryAddStatusEffect<FlagBuffComponent>(entityInRange, CourageEffectKey, flagComp.timespan, true);
            }
        }

        private void ModifyDamage(EntityUid uid, FlagBuffComponent comp, MeleeHitEvent args)
        {
            /*
                This is a little bit cheaty but I couldn't
                figure out how to specify a damagemodifierset
                inside a component to actually modify the damage
                so for now I'm just adding the base damage to
                bonus damage
            */
            args.BonusDamage += args.BaseDamage;
        }

        public bool GetRole(MindComponent mind, String compare)
        {
            if (mind.Mind is null)
                return false;

            foreach (var role in mind.Mind.AllRoles)
            {
                if (role is not TraitorRole traitor)
                    continue;
                if (traitor.Prototype.ID == compare)
                {
                    return true;
                }
            }
            return false;
        }

        public override void Update(float frametime)
        {
            base.Update(frametime);
            foreach (var flagComp in EntityQuery<FlagComponent>())
            {
                flagComp.accumulator += frametime;
                if (flagComp.accumulator > 0.1)
                {
                    flagComp.accumulator = 0;
                    Aura(flagComp, flagComp.range);
                }
            }
        }
    }
