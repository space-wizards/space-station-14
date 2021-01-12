using Content.Server.GameObjects.Components.Projectiles;
using Content.Server.Utility;
using Content.Shared.Actions;
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

        public bool Stationary { get; private set; }
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
            serializer.DataField(this, x => x.Stationary, "trap", false); //Apply or not apply momentum to the projectile (If true, will spawn a stationary trap-like spell)
            serializer.DataField(this, x => x.VelocityMult, "speed", 10f); //Speed that is applied to the projectile
            serializer.DataField(this, x => x.CoolDown, "cooldown", 0f) ;
            serializer.DataField(this, x => x.IgnoreCaster, "ignorecaster", false); //ignore caster or not
        }

        public void DoTargetPointAction(TargetPointActionEventArgs args)
        {
            var playerPosition = args.Performer.Transform.LocalPosition;
            var direction = (args.Target.Position - playerPosition).Normalized * 2; //Decides the general direction of the spell (used later) + how far it goes
            var coords = args.Performer.Transform.Coordinates.WithPosition(playerPosition + direction);

            args.Performer.PopupMessageEveryone(CastMessage); //Speak the cast message

            var spawnedSpell = _entityManager.SpawnEntity(Projectile, coords);

            var physics = spawnedSpell.GetComponent<IPhysicsComponent>();
            physics.Status = BodyStatus.InAir;

            var projectileComponent = spawnedSpell.GetComponent<ProjectileComponent>();
            if (IgnoreCaster == true)
            {
                projectileComponent.IgnoreEntity(args.Performer);
            }
            

            if (Stationary == true) //If the spell is a trap, the lower code won't apply
            {
                return;
            }

            spawnedSpell
                .GetComponent<IPhysicsComponent>()
                .EnsureController<BulletController>()
                .LinearVelocity = direction * VelocityMult;

            spawnedSpell.Transform.LocalRotation = args.Performer.Transform.LocalRotation;
            //Actions.Cooldown(args.ActionType, Cooldowns.SecondsFromNow(CoolDown));
        }
    }
}
