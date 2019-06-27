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
                case LimbState.Injured:
                    color = new Color(255, 255, 0);
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
            switch (ChangeHealthValue(damage))
            {
                case LimbState.Healthy:
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
                case LimbState.Missing:
                    break;
            }
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

            switch (CurrentHealth)
            {
                case int n when (n > MaxHealth / 2):
                    State = LimbState.Healthy;
                    break;
                case int n when (n <= MaxHealth / 2 && n > MaxHealth / 3):
                    State = LimbState.Injured;
                    break;
                case int n when (n <= MaxHealth / 3 && n > 0):
                    State = LimbState.InjuredSeverely;
                    break;
                case int n when (n == 0):
                    State = LimbState.Missing;
                    break;
                default:
                    break;

            }
            return State;
        }
    }

    public enum LimbState
    {
        Healthy,
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
