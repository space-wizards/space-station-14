using Content.Server.GameObjects.Components.Explosion;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Server.GameObjects.Components.Projectiles
{
    [DataDefinition]
    [RegisterComponent]
    public class MagicalProjectileComponent : Component, IStartCollide
    {
        public override string Name => "MagicalProjectile";

        [ViewVariables] [DataField("NeedComponent")] public string TargetType { get; set; } = "Mind";

        [ViewVariables] [DataField("AddedComponent")] public string InduceComponent { get; set; } = "RadiatonPulse";

        [ViewVariables] [DataField("duration")] public int SpellDuration { get; set; } = 100;

        [ViewVariables] [DataField("castsound")] private string? _castsound = "/Audio/Weapons/emitter.ogg";


        public Type? RegisteredTargetType;

        public Type? RegisteredInduceType;
      

        public override void Initialize()
        {
            base.Initialize();

        }

        void IStartCollide.CollideWith(Fixture ourFixture, Fixture otherFixture, in Manifold manifold)
        {
            if (otherFixture == null) return;
            var target = otherFixture.Body.Owner;
            var compFactory = IoCManager.Resolve<IComponentFactory>();
            var registration = compFactory.GetRegistration(TargetType);
            RegisteredTargetType = registration.Type;
            //Inducer registration
            var registrationInducer = compFactory.GetRegistration(InduceComponent);
            RegisteredInduceType = registrationInducer.Type;
            if (!target.TryGetComponent(RegisteredTargetType, out var component))
            {
                return;
            }
            if (target.HasComponent(RegisteredInduceType))
            {
                return;
            }
            var componentInduced = compFactory.GetComponent(RegisteredInduceType);
            Component compInducedFinal = (Component) componentInduced;
            compInducedFinal.Owner = target;
            target.EntityManager.ComponentManager.AddComponent(target, compInducedFinal);
            target.SpawnTimer(SpellDuration, () => target.EntityManager.ComponentManager.RemoveComponent(target.Uid, compInducedFinal));
            if (_castsound != null)
            {
                SoundSystem.Play(Filter.Pvs(Owner), _castsound, Owner);
            }
            else return;
        }
    }
}
