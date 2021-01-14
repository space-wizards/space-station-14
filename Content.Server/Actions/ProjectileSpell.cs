using Content.Server.GameObjects.Components.Projectiles;
using Content.Server.Utility;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Physics;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Server.Actions
{
    [UsedImplicitly]
    public class ProjectileSpell : ITargetPointAction
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public string CastMessage { get; private set; }
        public string Projectile { get; private set; }
        public float VelocityMult { get; private set; }
        public float CoolDown { get; private set; }
        public bool IgnoreCaster { get; private set; }

        public ProjectileSpell()
        {
            IoCManager.InjectDependencies(this);
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.CastMessage, "castmessage", "Instant action used."); //What player says upon casting the spell
            serializer.DataField(this, x => x.Projectile, "spellprojectile", null); //What projectile/Entity does the spell create
            serializer.DataField(this, x => x.VelocityMult, "speed", 0f); //Speed that is applied to the projectile
            serializer.DataField(this, x => x.CoolDown, "cooldown", 0f);
            serializer.DataField(this, x => x.IgnoreCaster, "ignorecaster", false); //ignore caster or not
        }

        public void DoTargetPointAction(TargetPointActionEventArgs args)
        {
            if (!args.Performer.TryGetComponent<SharedActionsComponent>(out var actions)) return;
            actions.Cooldown(args.ActionType, Cooldowns.SecondsFromNow(CoolDown)); //Set the spell on cooldown
            var playerPosition = args.Performer.Transform.LocalPosition; //Set relative position of the entity of the spell (used later)
            var direction = (args.Target.Position - playerPosition).Normalized * 2; //Decides the general direction of the spell (used later) + how far it goes
            var coords = args.Performer.Transform.Coordinates.WithPosition(playerPosition + direction);

            args.Performer.PopupMessageEveryone(CastMessage); //Speak the cast message out loud

            var spawnedSpell = _entityManager.SpawnEntity(Projectile, coords);

            if (spawnedSpell.TryGetComponent<ProjectileComponent>(out var projectileComponent)) //If it is specifically a projectile entity, then make it ignore the caster optionally
            {
                if (IgnoreCaster == true) //ignores or not the caster
                {
                    projectileComponent.IgnoreEntity(args.Performer);
                }
                spawnedSpell
                .GetComponent<IPhysicsComponent>()
                .EnsureController<BulletController>()
                .LinearVelocity = direction * VelocityMult;
            }
              spawnedSpell.Transform.LocalRotation = args.Performer.Transform.LocalRotation;
        }
    }
}
