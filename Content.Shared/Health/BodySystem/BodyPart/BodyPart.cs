using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;



namespace Content.Shared.BodySystem
{


    /// <summary>
    ///     Data class representing a singular limb such as an arm or a leg. Contains functions to manipulate this data.
    ///     Typically held within a BodyManagerComponent, which coordinates functions between BodyParts.
    /// </summary>
    [NetSerializable, Serializable]
    public class BodyPart
    {

        [ViewVariables]
        private List<Mechanism> _mechanisms = new List<Mechanism>();

        [ViewVariables]
        private ISurgeryData _surgeryData;

        [ViewVariables]
        private int _sizeUsed = 0;

        /// <summary>
        ///     Body part name.
        /// </summary>
        [ViewVariables]
        public string Name;

        /// <summary>
        ///     Plural version of this body part's name.
        /// </summary>
        [ViewVariables]
        public string Plural;

        /// <summary>
        ///     Path to the RSI that represents this BodyPart.
        /// </summary>			  
        [ViewVariables]
        public string RSIPath;

        /// <summary>
        ///     RSI state that represents this BodyPart.
        /// </summary>			  
        [ViewVariables]
        public string RSIState;

        /// <summary>
        ///     BodyPartType that this body part is considered. 
        /// </summary>
        [ViewVariables]
        public BodyPartType PartType;

        /// <summary>
        ///     Max HP of this body part.
        /// </summary>		
        [ViewVariables]
        public int MaxDurability;

        /// <summary>
        ///     Current HP of this body part based on sum of all damage types.
        /// </summary>		
        [ViewVariables]
        public int CurrentDurability => MaxDurability - CurrentDamages.Damage;

        /// <summary>
        ///     Current damage dealt to this BodyPart.
        /// </summary>		
        [ViewVariables]
        public AbstractDamageContainer CurrentDamages;

        /// <summary>
        ///     At what HP this body part is completely destroyed.
        /// </summary>		
        [ViewVariables]
        public int DestroyThreshold;

        /// <summary>
        ///     Armor of the body part against attacks.
        /// </summary>		
        [ViewVariables]
        public float Resistance;

        /// <summary>
        ///     Determines many things: how many mechanisms can be fit inside a body part, fitting through tiny crevices, etc.
        /// </summary>		
        [ViewVariables]
        public int Size;

        /// <summary>
        ///     What types of body parts this body part can attach to. For the most part, most limbs aren't universal and require extra work to attach between types.
        /// </summary>
        [ViewVariables]
        public BodyPartCompatibility Compatibility;

        /// <summary>
        ///     List of IExposeData properties, allowing for additional data classes to be attached to a limb, such as a "length" class to an arm.
        /// </summary>
        [ViewVariables]
        public List<IExposeData> Properties;

        /// <summary>
        ///     List of all Mechanisms currently inside this BodyPart.
        /// </summary>
        [ViewVariables]
        public List<Mechanism> Mechanisms => _mechanisms;


        public BodyPart(BodyPartPrototype data)
        {
            LoadFromPrototype(data);
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
        public bool AttemptSurgery(SurgeryToolType toolType, IEntity performer)
        {
            return _surgeryData.PerformSurgery(toolType, performer);
        }

        /// <summary>
        ///     Attempts to add a Mechanism. Returns true if successful, false if there was an error (e.g. not enough room in BodyPart)
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
        ///     Removes a given Mechanism from this BodyPart. Returns false if there was an error (like the given mechanism not actually being inside this BodyPart).
        /// </summary>
        public bool RemoveMechanism(Mechanism mechanism)
        {
            if (!_mechanisms.Contains(mechanism))
                return false;
            _mechanisms.Remove(mechanism);
            _sizeUsed -= mechanism.Size;
            return true;
        }



        /// <summary>
        ///    Loads the given BodyPartPrototype - current data on this BodyPart will be overwritten!
        /// </summary>	
        public void LoadFromPrototype(BodyPartPrototype data)
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
            //_surgeryData = (ISurgeryData) Activator.CreateInstance(null, data.SurgeryDataName);
            _surgeryData = new BiologicalSurgeryData(this);
            Properties = data.Properties;
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
