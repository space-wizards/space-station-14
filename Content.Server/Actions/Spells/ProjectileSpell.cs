using Content.Server.GameObjects.Components.Projectiles;
using Content.Server.Utility;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Physics;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Server.Actions
{
    [UsedImplicitly]
    [DataDefinition]
    public class ProjectileSpell : ITargetPointAction
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        [ViewVariables] [DataField("castmessage")] public string CastMessage { get; set; } = "Instant action used.";
        [ViewVariables] [DataField("spellprojectile")] public string Projectile { get; set; } = "FireBallbullet";
        [ViewVariables] [DataField("speed")] public float VelocityMult { get; set; } = 0f;
        [ViewVariables] [DataField("cooldown")] public float CoolDown { get; set; } = 1f;
        [ViewVariables] [DataField("ignorecaster")] public bool IgnoreCaster { get; set; } = false;

        [ViewVariables] [DataField("castsound")] public string? _castsound { get; set; } = "/Audio/Weapons/emitter.ogg";

        public ProjectileSpell()
        {
            IoCManager.InjectDependencies(this);
        }

        public void DoTargetPointAction(TargetPointActionEventArgs args)
        {
            var caster = args.Performer;
            if (!caster.TryGetComponent<SharedActionsComponent>(out var actions)) return;
            actions.Cooldown(args.ActionType, Cooldowns.SecondsFromNow(CoolDown)); //Set the spell on cooldown
            var playerPosition = args.Performer.Transform.LocalPosition; //Set relative position of the entity of the spell (used later)
            var direction = (args.Target.Position - playerPosition).Normalized * 2; //Decides the general direction of the spell (used later) + how far it goes
            var coords = args.Performer.Transform.Coordinates.WithPosition(playerPosition + direction);

            caster.PopupMessageEveryone(CastMessage); //Speak the cast message out loud

            var spawnedSpell = _entityManager.SpawnEntity(Projectile, coords);

            if (spawnedSpell.TryGetComponent<ProjectileComponent>(out var projectileComponent)) //If it is specifically a projectile entity, then make it ignore the caster optionally
            {
                if (IgnoreCaster == true) //ignores or not the caster
                {
                    projectileComponent.IgnoreEntity(args.Performer);
                }
                spawnedSpell
                .GetComponent<PhysicsComponent>()
                .LinearVelocity = direction * VelocityMult;
            }
              spawnedSpell.Transform.LocalRotation = args.Performer.Transform.LocalRotation;
            if (_castsound != null)
            {
                SoundSystem.Play(Filter.Pvs(caster), _castsound, caster);
            }
            else return;
        }
    }
}
