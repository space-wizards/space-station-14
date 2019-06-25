using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    /// <summary>
    ///     Organ acts just like component - it gets called by main Life() function of the body and have to process the data on tick
    /// </summary>

    public abstract class Organ //: IPrototype TODO: when YAML comes, i have to fix "No PrototypeAttribute to give it a type string."
    {
        public string Name;

        public int MaxHealth;

        public int CurrentHealth;

        public float BloodChange = 0;

        public OrganState State = OrganState.Healthy;

        public List<OrganStatus> Statuses;

        public Dictionary<string, object> OrganData; //TODO

        public IEntity Owner;

        public virtual void mockInit(string name, int health, OrganState state, IEntity owner) //Temp code before YAML 
        {
            Name = name;
            MaxHealth = health;
            CurrentHealth = MaxHealth;
            State = state;
            Statuses = new List<OrganStatus>();
            Owner = owner;
            ApplyOrganData();
            Startup();
        }

        public virtual void LoadFrom(YamlMappingNode mapping)
        {

        }
        public virtual void Startup()
        {

        }
        public virtual void ApplyOrganData()
        {

        }

        public abstract void Life();

        public void HandleDamage(int damage) //TODO: test prob numbers
        {
            CurrentHealth -= damage;
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
                    State = OrganState.Healthy;
                    break;
                case int n when (n < MaxHealth / 2 && n > 0):
                    State = OrganState.Damaged;
                    break;
                case int n when (n == 0):
                    State = OrganState.Missing;
                    break;
                default:
                    break;

            }
            Logger.DebugS("Organ", "Organ {0} received {1} damage!", Name, damage);
        }

        public virtual Blood CirculateBlood(Blood blood)
        {
            blood.ChangeCurrentVolume(BloodChange);
            return blood;
        }
    }

    public enum OrganState
    {
        Healthy,
        Damaged,
        Dead,
        Missing
    }

    public enum OrganStatus
    {
        Bleeding,
        Boost,
        Sepsis,
        Cancer
    }
}
