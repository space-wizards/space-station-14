using System;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Content.Server.Interfaces;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.GameObjects.Components.Mobs.Body;
using Content.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs
{
    public class BodyComponent : Component, IExAct, IOnDamageReceived
    {
#pragma warning disable CS0649
        [Dependency]
        private readonly IPrototypeManager PrototypeManager;
#pragma warning restore

        public override string Name => "Body";

        public BodyInstance Body;
        private string bodyProto;

        Random Seed;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref bodyProto, "template", "");
        }

        public override void Initialize()
        {
            base.Initialize();
            Body = PrototypeManager.Index<BodyPrototype>(bodyProto).Create();
            Body.Initialize(Owner, PrototypeManager);
            Seed = new Random(DateTime.Now.GetHashCode() ^ Owner.GetHashCode());
        }

        void Update(float frameTime)
        {
            Body.Life(frameTime);
        }

        void IOnDamageReceived.OnDamageReceived(OnDamageReceivedEventArgs e)
        {
            Body.HandleDamage(e.DamageType, e.Damage);
        }

        void IExAct.OnExplosion(ExplosionEventArgs eventArgs)
        {
            var burnDamage = 0;
            var bruteDamage = 0;
            switch (eventArgs.Severity)
            {
                case ExplosionSeverity.Destruction:
                    DestroyOwner();
                    break;
                case ExplosionSeverity.Heavy:
                    if (Seed.Prob(0.4f))
                    {
                        DestroyOwner();
                    }
                    else
                    {
                        bruteDamage += 60;
                        burnDamage += 60;
                    }
                    break;
                case ExplosionSeverity.Light:
                    bruteDamage += 30;
                    break;
            }
            if (bruteDamage > 0)
            {
                Owner.GetComponent<DamageableComponent>().TakeDamage(DamageType.Brute, bruteDamage);
            }
            if (burnDamage > 0)
            {
                Owner.GetComponent<DamageableComponent>().TakeDamage(DamageType.Heat, burnDamage);
            }
        }

        private void DestroyOwner()
        {
            Body.Gib();
            if (Owner.TryGetComponent<MindComponent>(out var mindComponent))
            {
                var ghost = Owner.EntityManager.ForceSpawnEntityAt("MobObserver", Owner.Transform.GridPosition);
                var mind = mindComponent.Mind;
                mind.UnVisit();
                mind.Visit(ghost);
            }
            Owner.Delete();
        }
    }
}
