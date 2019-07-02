using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Content.Shared.GameObjects;
using Content.Server.GameObjects.Components.Mobs.Body.Organs;


namespace Content.Server.GameObjects.Components.Mobs.Body
{
    /// <summary>
    ///     Limb is not just like <see cref="Organ"/>, it has BONES, and holds organs (and child limbs), 
    ///     it receive damage first, then through resistances and such it transfers the damage to organs,
    ///     also the limb is visible, and it can be targeted
    /// </summary>
    public class Limb : BodyPart
    {
#pragma warning disable CS0649
        [Dependency]
        protected IPrototypeManager PrototypeManager;
#pragma warning restore

        public string TexturePath { get; private set; }
        public string Parent { get; private set; }

        public List<Organ> Organs = new List<Organ>();
        public List<Limb> Children = new List<Limb>();

        bool childOrganDamage; //limbs that are used for deattaching, they'll get deleted if parent is dropped. (arms, legs)
        bool directOrganDamage;

        public List<string> OrganPrototypes { get; private set; }

        public void DataFromPrototype(LimbPrototype prototype)
        {
            Id = prototype.Id;
            Name = prototype.Name;
            MaxHealth = prototype.MaxHealth;
            CurrentHealth = MaxHealth;
            TexturePath = prototype.TexturePath;
            PrototypeEntity = prototype.PrototypeEntity;
            OrganPrototypes = prototype.OrganPrototypes;
            childOrganDamage = prototype.ChildOrganDamage;
            directOrganDamage = prototype.DirectOrganDamage;
            Parent = prototype.Parent;
        }

        public LimbRender Render() //TODO
        {
            var color = new Color(128, 128, 128);
            switch (State)
                {
                case BodyPartState.Healthy:
                    color = new Color(0, 255, 0);
                    break;
                case BodyPartState.InjuredLightly:
                    color = new Color(255, 255, 0);
                    break;
                case BodyPartState.Injured:
                    color = new Color(255, 165, 0);
                    break;
                case BodyPartState.InjuredSeverely:
                    color = new Color(255, 0, 0);
                    break;
                case BodyPartState.Dead:
                    color = new Color(128, 128, 128);
                    break;
                }
            return new LimbRender(TexturePath, color);

        }

        public override void HandleGib()
        {
            if(directOrganDamage) //don't spawn both arm and hand, leg and foot
            {
                return;
            }
            base.HandleGib();
        }

        public void HandleDecapitation(bool spawn)
        {
            foreach (var child in Children)
            {
                child.HandleDecapitation(childOrganDamage);
            }
            if (spawn)
            {
                SpawnPrototype(PrototypeEntity);
            }
            BodyOwner.RemovePart(this);
        }

        public override void HandleDamage(int damage) //TODO: test prob numbers, and add targetting!
        {
            if (CurrentHealth - damage < 0)
            {
                CurrentHealth = 0;
            } 
            else if (CurrentHealth - damage > MaxHealth)
            {
                CurrentHealth = MaxHealth;
            }
            else
            {
                CurrentHealth -= damage;
            }

            if(Organs.Count == 0)
            {
                return;
            }
            switch (State)
            {
                case BodyPartState.InjuredLightly:
                    if(_seed.Prob(0.1f))
                    {
                        _seed.Pick(Organs).HandleDamage(damage);
                    }
                    break;
                case BodyPartState.Injured:
                    //Organs[0].HandleDamage(damage); //testing brain damage
                    if (_seed.Prob(0.4f))
                    {
                        _seed.Pick(Organs).HandleDamage(damage);
                    }
                    break;
                case BodyPartState.InjuredSeverely:
                    _seed.Pick(Organs).HandleDamage(damage);
                    break;
                case BodyPartState.Dead:
                    HandleDecapitation(true);
                    break;
            }
            Logger.DebugS("Limb", "Limb {0} received {1} damage!", Name, damage);
        }
    }
}
