using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Log;
using Robust.Shared.Maths;

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

        public float BloodChange = 0.005f; //TODO: Organs should consume reagents (nutriments) from blood, not blood directly

        public OrganState State = OrganState.Healthy;

        public List<OrganStatus> Statuses;

        public Dictionary<string, object> OrganData; //TODO

        public IEntity Owner;

        public string PrototypeEnitity; //entity that spawns on place of the organ, useful for gibs and surgery

        public string GibletEntity;

        public BodyTemplate Body;

        Random _seed;

        public virtual void mockInit(string name, int health, OrganState state, IEntity owner, BodyTemplate body, string prototype) //Temp code before YAML 
        {
            Name = name;
            MaxHealth = health;
            CurrentHealth = MaxHealth;
            State = state;
            Statuses = new List<OrganStatus>();
            Owner = owner;
            Body = body;
            PrototypeEnitity = prototype;
            ApplyOrganData();
            Startup();
        }

        public virtual void LoadFrom(YamlMappingNode mapping)
        {

        }
        public virtual void Startup()
        {
            _seed = new Random(DateTime.Now.GetHashCode());
            GibletEntity = _seed.Pick(new List<string> { "Gib01", "Gib02", "Gib03", "Gib04", "Gib05" }); //HACK, they should be snowflakey decals which we don't have rn
        }
        public virtual void ApplyOrganData()
        {

        }

        public virtual void Life(int lifeTick)
        {

        }

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
                    HandleStateChange(OrganState.Healthy);
                    break;
                case int n when (n <= MaxHealth / 2 && n > 0):
                    HandleStateChange(OrganState.Damaged);
                    break;
                case int n when (n == 0):
                    HandleStateChange(OrganState.Dead);
                    break;
            }

            Logger.DebugS("Organ", "Organ {0} received {1} damage!", Name, damage);
        }

        public void HandleGib()
        {
            if (_seed.Prob(0.5f))
            {
                SpawnPrototype(GibletEntity);
            } else if (_seed.Prob(0.6f))
            {
                SpawnPrototype(PrototypeEnitity);
            }
            //Dispose();
        }

        public void SpawnPrototype(string Prototype)
        {
            if (!string.IsNullOrWhiteSpace(Prototype))
            {
                Owner.EntityManager.TrySpawnEntityAt(Prototype, Owner.Transform.GridPosition, out var entity);
            }
        }

        private void Dispose()
        {
            Owner = null;
            Body = null;
        }

        public virtual Blood CirculateBlood(Blood blood)
        {
            blood.ChangeCurrentVolume(BloodChange);
            return blood;
        }

        private void HandleStateChange(OrganState newState) //called once the state is changed
        {
            if (!newState.Equals(State))
            {
                var oldState = State;
                State = newState;
                OnStateChange(oldState);
            }
        }

        public virtual void OnStateChange(OrganState oldState)
        {

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
