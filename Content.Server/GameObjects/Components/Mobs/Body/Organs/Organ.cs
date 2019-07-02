using Robust.Shared.Log;
using YamlDotNet.RepresentationModel;

namespace Content.Server.GameObjects.Components.Mobs.Body.Organs
{
    /// <summary>
    ///     Organ acts just like component - it gets called by main Life() function of the body and have to process the data on tick
    /// </summary>

    public abstract class Organ : BodyPart
    {
        public virtual void Startup() 
        {
            CurrentHealth = MaxHealth;
        }

        public void DataFromPrototype(OrganPrototype prototype)
        {
            Id = prototype.Id;
            Name = prototype.Name;
            MaxHealth = prototype.MaxHealth;
            CurrentHealth = MaxHealth;
            PrototypeEntity = prototype.PrototypeEntity;
        }

        public virtual void ExposeData(YamlMappingNode mapping) { }

        public override void HandleDamage(int damage) //TODO: test prob numbers
        {
            var state = State;
            CurrentHealth -= damage;
            if (CurrentHealth < 0)
            {
                CurrentHealth = 0;
            }
            if (CurrentHealth > MaxHealth)
            {
                CurrentHealth = MaxHealth;
            }

            HandleStateChange(state);

            Logger.DebugS("Organ", "Organ {0} received {1} damage!", Name, damage);
        }

        private void HandleStateChange(BodyPartState oldState) //called once the state is changed
        {
            if (!oldState.Equals(State))
            {
                OnStateChange(oldState);
            }
        }

        public virtual void OnStateChange(BodyPartState oldState) { }
    }
}
