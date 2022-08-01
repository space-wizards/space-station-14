using Content.Server.RevolutionFlag.Components;
using Content.Server.Mind.Components;
using Content.Server.Traitor;
using Content.Server.Weapon.Melee;
using Content.Shared.Damage;
using Content.Shared.Hands;

namespace Content.Server.RevolutionFlag;
    internal sealed class FlagSystem : EntitySystem
    {
        [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
        private const string RevolutionaryPrototypeId = "Revolutionary";
        private List<EntityUid> UnderEffect = new();
        private DamageModifierSet Modifiers = default!; 
        
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MindComponent,MeleeHitEvent>(ModifyDamage);
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

        public void Aura(FlagComponent flagcomp, float range)
        {
            var transform = EntityManager.GetComponent<TransformComponent>(flagcomp.Owner);
            var mapPosition = transform.MapPosition;
            var inRange = _entityLookup.GetEntitiesInRange(transform.Coordinates, range);

            UnderEffect.Clear();

            if (!flagcomp.active)
                return;
            foreach(var entityInRange in inRange)
            {
                if (!TryComp<MindComponent>(entityInRange, out var mind))
                    continue;
                if (!GetRole(mind, RevolutionaryPrototypeId))
                    continue;
                UnderEffect.Add(entityInRange);
            }
        }

        private void ModifyDamage(EntityUid uid, MindComponent comp, MeleeHitEvent args)
        {
            if (!UnderEffect.Contains(uid))
                return;
            args.BonusDamage = DamageSpecifier.ApplyModifierSet(args.BaseDamage + new DamageSpecifier(), Modifiers);
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
                if (flagComp.accumulator > 1)
                {
                    flagComp.accumulator = 0;
                    Modifiers = flagComp.Modifiers;
                    Aura(flagComp, flagComp.range);
                }
            }
        }
    }