using Content.Shared.BodySystem;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;



namespace Content.Server.BodySystem
{


    /// <summary>
    ///     Data class representing a singular limb such as an arm or a leg. Typically held within a BodyManagerComponent,
    ///     which coordinates functions between BodyParts.
    /// </summary>
    public class BodyPart
    {

        [ViewVariables]
        private ISurgeryData _surgeryData;

        [ViewVariables]
        private List<Mechanism> _mechanisms = new List<Mechanism>();

        [ViewVariables]
        private int _sizeUsed = 0;

        /// <summary>
        ///     Body part name.
        /// </summary>
        [ViewVariables]
        public string Name { get; set; }

        /// <summary>
        ///     Plural version of this body part's name.
        /// </summary>
        [ViewVariables]
        public string Plural { get; set; }

        /// <summary>
        ///     Path to the RSI that represents this BodyPart.
        /// </summary>			  
        [ViewVariables]
        public string RSIPath { get; set; }

        /// <summary>
        ///     RSI state that represents this BodyPart.
        /// </summary>			  
        [ViewVariables]
        public string RSIState { get; set; }

        /// <summary>
        ///     BodyPartType that this body part is considered. 
        /// </summary>
        [ViewVariables]
        public BodyPartType PartType { get; set; }

        /// <summary>
        ///     Max HP of this body part.
        /// </summary>		
        [ViewVariables]
        public int MaxDurability { get; set; }

        /// <summary>
        ///     Current HP of this body part based on sum of all damage types.
        /// </summary>		
        [ViewVariables]
        public int CurrentDurability => MaxDurability - CurrentDamages.Damage;

        /// <summary>
        ///     Current damage dealt to this BodyPart.
        /// </summary>		
        [ViewVariables]
        public AbstractDamageContainer CurrentDamages { get; set; }

        /// <summary>
        ///     At what HP this body part is completely destroyed.
        /// </summary>		
        [ViewVariables]
        public int DestroyThreshold { get; set; }

        /// <summary>
        ///     Armor of the body part against attacks.
        /// </summary>		
        [ViewVariables]
        public float Resistance { get; set; }

        /// <summary>
        ///     Determines many things: how many mechanisms can be fit inside a body part, fitting through tiny crevices, etc.
        /// </summary>		
        [ViewVariables]
        public int Size { get; set; }

        /// <summary>
        ///     What types of body parts this body part can attach to. For the most part, most limbs aren't universal and require extra work to attach between types.
        /// </summary>
        [ViewVariables]
        public BodyPartCompatibility Compatibility { get; set; }

        /// <summary>
        ///     List of IExposeData properties, allowing for additional data classes to be attached to a limb, such as a "length" class to an arm.
        /// </summary>
        [ViewVariables]
        public List<IExposeData> Properties { get; set; }

        /// <summary>
        ///     List of all Mechanisms currently inside this BodyPart.
        /// </summary>
        [ViewVariables]
        public List<Mechanism> Mechanisms => _mechanisms;

        public BodyPart(){}

        public BodyPart(BodyPartPrototype data)
        {
            LoadFromPrototype(data);
        }




        /// <summary>
        ///     Attempts to add a Mechanism. Returns true if successful, false if there was an error (e.g. not enough room in BodyPart). Use InstallDroppedMechanism if you want to easily install an IEntity with a DroppedMechanismComponent.
        /// </summary>
        public bool InstallMechanism(Mechanism mechanism)
        {
            if (_sizeUsed + mechanism.Size > Size)
                return false; //No space
            _mechanisms.Add(mechanism);
            _sizeUsed += mechanism.Size;
            return true;
        }

        /// <summary>
        ///     Attempts to install a DroppedMechanismComponent into the given limb, potentially deleting the dropped IEntity. Returns true if successful, false if there was an error (e.g. not enough room in BodyPart).
        /// </summary>
        public bool InstallDroppedMechanism(DroppedMechanismComponent droppedMechanism)
        {
            if (_sizeUsed + droppedMechanism.ContainedMechanism.Size > Size)
                return false; //No space
            InstallMechanism(droppedMechanism.ContainedMechanism);
            droppedMechanism.Owner.Delete();
            return true;
        }

        /// <summary>
        ///     Tries to remove the given Mechanism reference from the given BodyPart reference. Returns null if there was an error in spawning the entity or removing the mechanism, otherwise returns a reference to the DroppedMechanismComponent on the newly spawned entity.
        /// </summary>	
        public DroppedMechanismComponent DropMechanism(IEntity dropLocation, Mechanism mechanismTarget)
        {
            if (!_mechanisms.Contains(mechanismTarget))
                return null;
            _mechanisms.Remove(mechanismTarget);
            _sizeUsed -= mechanismTarget.Size;
            IEntityManager entityManager = IoCManager.Resolve<IEntityManager>();
            var mechanismEntity = entityManager.SpawnEntity("BaseDroppedMechanism", dropLocation.Transform.GridPosition);
            var droppedMechanism = mechanismEntity.GetComponent<DroppedMechanismComponent>();
            droppedMechanism.InitializeDroppedMechanism(mechanismTarget);
            return droppedMechanism;
        }

        /// <summary>
        ///     Tries to destroy the given Mechanism in the given BodyPart. Returns false if there was an error, true otherwise. Does NOT spawn a dropped entity.
        /// </summary>	
        public bool DestroyMechanism(BodyPart bodyPartTarget, Mechanism mechanismTarget)
        {
            if (!_mechanisms.Contains(mechanismTarget))
                return false;
            _mechanisms.Remove(mechanismTarget);
            _sizeUsed -= mechanismTarget.Size;
            return true;
        }

        /// <summary>
        ///     Returns whether the given SurgertToolType can be used on the current state of this BodyPart (e.g. 
        /// </summary>
        public bool SurgeryCheck(SurgeryToolType toolType)
        {
            return _surgeryData.CheckSurgery(toolType);
        }

        /// <summary>
        ///     Attempts to perform surgery on this BodyPart with the given tool. Returns false if there was an error, true if successful.
        /// </summary>
        public bool AttemptSurgery(SurgeryToolType toolType, BodyManagerComponent target, IEntity performer)
        {
            return _surgeryData.PerformSurgery(toolType, target, performer);
        }

        /// <summary>
        ///    Loads the given BodyPartPrototype - current data on this BodyPart will be overwritten!
        /// </summary>	
        public virtual void LoadFromPrototype(BodyPartPrototype data)
        {
            Name = data.Name;
            Plural = data.Plural;
            PartType = data.PartType;
            RSIPath = data.RSIPath;
            RSIState = data.RSIState;
            MaxDurability = data.Durability;
            CurrentDamages = new BiologicalDamageContainer();
            Resistance = data.Resistance;
            Size = data.Size;
            Compatibility = data.Compatibility;
            Properties = data.Properties;
            //_surgeryData = (ISurgeryData) Activator.CreateInstance(null, data.SurgeryDataName);
            //TODO: figure out a way to convert a string name in the YAML to the proper class (reflection won't work for reasons)
            _surgeryData = new BiologicalSurgeryData(this);
            IPrototypeManager prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            foreach (string mechanismPrototypeID in data.Mechanisms)
            {
                if (!prototypeManager.TryIndex(mechanismPrototypeID, out MechanismPrototype mechanismData))
                {
                    throw new InvalidOperationException("No MechanismPrototype was found with the name " + mechanismPrototypeID + " while loading a BodyPartPrototype!");
                }
                _mechanisms.Add(new Mechanism(mechanismData));
            }

        }
    }
}
