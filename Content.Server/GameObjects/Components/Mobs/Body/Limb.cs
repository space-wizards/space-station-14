using System;
using Robust.Shared.Maths;
using System.Collections.Generic;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.Log;

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
        public float BloodChange = 0f;

        public void ExposeData(ObjectSerializer obj)
        {
            obj.DataField(ref Name, "name", "");
            obj.DataField(ref Id, "limb", null);
            obj.DataField(ref Organs, "organs", null);
            obj.DataField(ref MaxHealth, "health", 0);
        }
        public Limb(string name, Enum id, List<Organ> organs, List<Limb> children, int health)
        {
            Name = name;
            Id = id;
            Organs = organs;
            Children = children;
            MaxHealth = health;
            CurrentHealth = health;
            Statuses = new List<LimbStatus>();
        }

        public void Render() //TODO
        {

        }

        public void HandleDamage(int damage, Random seed) //TODO: test prob numbers
        {
            switch (ChangeHealthValue(damage))
            {
                case LimbState.Healthy:
                    if(seed.Prob(0.1f))
                    {
                        seed.Pick(Organs).HandleDamage(damage);
                    }
                    break;
                case LimbState.Injured:
                    //Organs[0].HandleDamage(damage); //testing brain damage
                    if (seed.Prob(0.4f))
                    {
                        seed.Pick(Organs).HandleDamage(damage);
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
                case int n when (n < MaxHealth / 2 && n > 0):
                    State = LimbState.Injured;
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
        Missing
    }

    public enum LimbStatus
    { 
        Bleeding,
        Broken
    }

}
