using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;

public enum BodyPartCompatibility { Mechanical, Biological, Universal };
public enum BodyPartType { Other, Torso, Head, Arm, Hand, Leg, Foot };


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
        ///     Path to the RSI sprite that represents this mechanism.
        /// </summary>			  
        [ViewVariables]
        public string SpritePath;

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
        public BodyPart(BodyPart duplicate)
        {

        }



        /// <summary>
        ///     Attempts to add a mechanism to this BodyPart. Returns true if successful, false if there was an error (e.g. not enough room in BodyPart)
        /// </summary>
        public bool AddMechanism(Mechanism mechanism)
        {
            return false;
        }

        /// <summary>
        ///     Removes a mechanism from this BodyPart. Returns the removed Mechanism if successful, null if not successful.
        /// </summary>
        public Mechanism RemoveMechanism(Mechanism mechanism)
        {
            return null;
        }



        /// <summary>
        ///    Loads the given BodyPartPrototype - current data on this BodyPart will be overwritten!
        /// </summary>	
        public void LoadFromPrototype(BodyPartPrototype data)
        {
            Name = data.Name;
            Plural = data.Plural;
            PartType = data.PartType;
            SpritePath = data.SpritePath;
            MaxDurability = data.Durability;
            CurrentDamages = new BiologicalDamageContainer();
            Resistance = data.Resistance;
            Size = data.Size;
            Compatibility = data.Compatibility;
            Properties = data.Properties;
            IPrototypeManager prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            foreach (string mechanismPrototypeID in data.Mechanisms)
            {
                if (!prototypeManager.TryIndex(mechanismPrototypeID, out MechanismPrototype mechanismData))
                {
                    throw new InvalidOperationException("No MechanismPrototype was found with the name " + mechanismPrototypeID + " while loading a BodyPartprototype!");
                }
                _mechanisms.Add(new Mechanism(mechanismData));
            }

        }
    }
}
