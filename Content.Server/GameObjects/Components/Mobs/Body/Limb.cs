using System;
using Robust.Shared.Maths;
using System.Collections.Generic;
using Robust.Shared.Serialization;
using Content.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    /// <summary>
    ///     Limb is not just like <see cref="Organ"/>, it has BONES, and holds organs (and child limbs), 
    ///     it receive damage first, then through resistances and such it transfers the damage to organs,
    ///     also the limb is visible, and it can be targeted
    /// </summary>
    public class Limb
    {
        public string Name;
        public List<Organ> Organs;
        System.Enum Id;
        public List<Limb> Children;
        public List<LimbStatus> Statuses;
        public LimbState State;
        int MaxHealth;
        int CurrentHealth;
        public IEntity Owner;
        public float BloodChange = 0f;
        public string PrototypeEntity = "";
        public string RenderLimb = "";
        public bool SnowflakeTarget; //limbs that are used for targeting, so they can't be deattached without parent (mouth, eyes)
        public bool SnowflakeParent; //limbs that are used for deattaching, they'll get deleted if parent is dropped. (arms, legs)

        Random _seed;

        public void ExposeData(ObjectSerializer obj)
        {
            obj.DataField(ref Name, "name", "");
            obj.DataField(ref Id, "limb", null);
            obj.DataField(ref Organs, "organs", null);
            obj.DataField(ref MaxHealth, "health", 0);
            obj.DataField(ref PrototypeEntity, "PrototypeEntity", "");
        }
        public Limb(string name, Enum id, List<Organ> organs, List<Limb> children, int health, string prototype, string render, IEntity owner, bool snowflakeT = false, bool snowflakeP = false)
        {
            Name = name;
            Id = id;
            Organs = organs;
            Children = children;
            MaxHealth = health;
            CurrentHealth = health;
            Statuses = new List<LimbStatus>();
            PrototypeEntity = prototype;
            RenderLimb = render;
            Owner = owner;
            SnowflakeTarget = snowflakeT;
            SnowflakeParent = snowflakeP;
            _seed = new Random(DateTime.Now.GetHashCode());
        }

        public LimbRender Render() //TODO
        {
            var color = new Color(128, 128, 128);
            switch (State)
                {
                case LimbState.Healthy:
                    color = new Color(0, 255, 0);
                    break;
                case LimbState.InjuredLightly:
                    color = new Color(255, 255, 0);
                    break;
                case LimbState.Injured:
                    color = new Color(255, 165, 0);
                    break;
                case LimbState.InjuredSeverely:
                    color = new Color(255, 0, 0);
                    break;
                case LimbState.Missing:
                    color = new Color(128, 128, 128);
                    break;
                }
            return new LimbRender(RenderLimb, color);

        }

        public void HandleGib()
        {
            if (_seed.Prob(0.7f) && !SnowflakeTarget && !SnowflakeTarget)
            {
                foreach (var organ in Organs)
                {
                    organ.HandleGib();
                }
            } else
            {
                if (!SnowflakeTarget)
                {
                    SpawnPrototypeEntity();
                }
            }
            Dispose();
        }

        public void HandleDecapitation(bool spawn)
        {
            State = LimbState.Missing;
            foreach (var organ in Organs)
            {
                organ.State = OrganState.Dead;
            }
            foreach (var child in Children)
            {
                child.HandleDecapitation(SnowflakeParent);
            }
            if (spawn)
            {
                SpawnPrototypeEntity();
            }
        }

        private void Dispose()
        {
            Children = null;
            Organs = null;
        }

        public void SpawnPrototypeEntity()
        {
            //TODO
            if (!string.IsNullOrWhiteSpace(PrototypeEntity))
            {
                Owner.EntityManager.TrySpawnEntityAt(PrototypeEntity, Owner.Transform.GridPosition, out var entity);
            }
        }

        public void HandleDamage(int damage) //TODO: test prob numbers
        {
            if (State == LimbState.Missing)
            {
                return; //It doesn't exist, so it's unaffected by damage/heal
            }
            var state = ChangeHealthValue(damage);
            switch (state)
            {
                case LimbState.InjuredLightly:
                    if(_seed.Prob(0.1f))
                    {
                        _seed.Pick(Organs).HandleDamage(damage);
                    }
                    break;
                case LimbState.Injured:
                    //Organs[0].HandleDamage(damage); //testing brain damage
                    if (_seed.Prob(0.4f))
                    {
                        _seed.Pick(Organs).HandleDamage(damage);
                    }
                    break;
                case LimbState.InjuredSeverely:
                    _seed.Pick(Organs).HandleDamage(damage);
                    break;
                case LimbState.Missing:
                    if (State != state)
                    {
                        HandleDecapitation(true);
                    }
                    break;
            }
            State = state;
            Logger.DebugS("Limb", "Limb {0} received {1} damage!", Name, damage);

        }

        public Blood CirculateBlood(Blood blood)
        {
            blood.ChangeCurrentVolume(BloodChange);
            return blood;
        }

        private LimbState ChangeHealthValue(int value)
        {
            CurrentHealth -= value;
            if (CurrentHealth < 0)
            {
                CurrentHealth = 0;
            }
            if (CurrentHealth > MaxHealth)
            {
                CurrentHealth = MaxHealth;
            }

            switch ((float)CurrentHealth)
            {
                case float n when (n > MaxHealth / 0.75f):
                    return LimbState.Healthy;
                case float n when (n <= MaxHealth / 0.75f && n > MaxHealth / 3f):
                    return LimbState.InjuredLightly;
                case float n when (n <= MaxHealth / 2f && n > MaxHealth / 4f):
                    return LimbState.Injured;
                case float n when (n <= MaxHealth / 4f && Math.Abs(n) > float.Epsilon):
                    return LimbState.InjuredSeverely;
                case float n when (Math.Abs(n) < float.Epsilon):
                    return LimbState.Missing;
            }
            return State;
        }
    }

    public enum LimbState
    {
        Healthy,
        InjuredLightly,
        Injured,
        InjuredSeverely,
        Missing
    }

    public enum LimbStatus
    { 
        Bleeding,
        Broken
    }
}
