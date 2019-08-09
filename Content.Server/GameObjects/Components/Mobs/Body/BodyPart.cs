using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    public abstract class BodyPart
    {
        protected Random _seed;

        public string Name { get; protected set; }
        public string Id { get; protected set; }
        public string PrototypeEntity { get; protected set; } //entity that spawns on place of the organ, useful for gibs and surgery
        public string GibletEntity { get; protected set; }

        public int MaxHealth { get; protected set; }
        public int CurrentHealth { get; protected set; }

        public BodyPartState State
        {
            get
            {
                switch (CurrentHealth)
                {
                    case int n when (n > MaxHealth * 0.9f):
                        return BodyPartState.Healthy;
                    case int n when (n <= MaxHealth * 0.9f && n > MaxHealth * 0.75f):
                        return BodyPartState.InjuredLightly;
                    case int n when (n <= MaxHealth * 0.5f && n > MaxHealth * 0.25f):
                        return BodyPartState.Injured;
                    case int n when (n <= MaxHealth / 0.25f && n > 0):
                        return BodyPartState.InjuredSeverely;
                    case int n when (n == 0):
                        return BodyPartState.Dead;
                }
                return BodyPartState.Dead;
            }
        }
        public IEntity Owner { get; private set; }
        public BodyInstance BodyOwner { get; private set; }

        public void Initialize(IEntity entity, BodyInstance body)
        {
            Owner = entity;
            BodyOwner = body;
            _seed = new Random(DateTime.Now.GetHashCode() ^ Owner.GetHashCode() ^ Name.GetHashCode() ^ Id.GetHashCode());
            GibletEntity = _seed.Pick(new List<string> { "Gib01", "Gib02", "Gib03", "Gib04", "Gib05" });

            StartUp();
        }

        public virtual void StartUp() { }

        public virtual void Life(float frameTime) { }

        public virtual void HandleDamage(int damage) { }

        public virtual void HandleGib() 
        {
            if (_seed.Prob(0.5f))
            {
                SpawnPrototype(GibletEntity);
            }
            else if (_seed.Prob(0.6f))
            {
                SpawnPrototype(PrototypeEntity);
            }
        }

        protected void SpawnPrototype(string Prototype)
        {
            if (!string.IsNullOrWhiteSpace(Prototype))
            {
                Owner.EntityManager.ForceSpawnEntityAt(Prototype, Owner.Transform.GridPosition);
            }
        }
    }

    public enum BodyPartState
    {
        Healthy = 4,
        InjuredLightly = 3,
        Injured = 2,
        InjuredSeverely = 1,
        Dead = 0
    }
}
