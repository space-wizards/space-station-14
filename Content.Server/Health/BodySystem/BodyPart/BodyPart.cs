using System;
using System.Collections.Generic;
using Content.Server.Health.BodySystem.Mechanism;
using Content.Server.Health.BodySystem.Surgery.Surgeon;
using Content.Server.Health.BodySystem.Surgery.SurgeryData;
using Content.Shared.Health.BodySystem;
using Content.Shared.Health.BodySystem.BodyPart;
using Content.Shared.Health.BodySystem.Mechanism;
using Content.Shared.Health.DamageContainer;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Health.BodySystem.BodyPart
{


    /// <summary>
    ///     Data class representing a singular limb such as an arm or a leg. Typically held within either a <see cref="BodyManagerComponent"/>,
    ///     which coordinates functions between BodyParts, or a <see cref="DroppedBodyPartComponent"/>.
    /// </summary>
    public class BodyPart
    {

        [ViewVariables]
        private ISurgeryData _surgeryData;

        [ViewVariables]
        private List<Mechanism.Mechanism> _mechanisms = new List<Mechanism.Mechanism>();

        [ViewVariables]
        private int _sizeUsed = 0;

        /// <summary>
        ///     The name of this BodyPart, often displayed to the user. For example, it could be named "advanced robotic arm".
        /// </summary>
        [ViewVariables]
        public string Name { get; set; }

        /// <summary>
        ///     Plural version of this BodyPart name.
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
        ///     <see cref="BodyPartType"/> that this BodyPart is considered to be. For example, BodyPartType.Arm.
        /// </summary>
        [ViewVariables]
        public BodyPartType PartType { get; set; }

        /// <summary>
        ///     Max HP of this BodyPart.
        /// </summary>
        [ViewVariables]
        public int MaxDurability { get; set; }

        /// <summary>
        ///     Current HP of this BodyPart based on sum of all damage types.
        /// </summary>
        [ViewVariables]
        public int CurrentDurability => MaxDurability - CurrentDamages.Damage;

        /// <summary>
        ///     Current damage dealt to this BodyPart.
        /// </summary>
        [ViewVariables]
        public AbstractDamageContainer CurrentDamages { get; set; }

        /// <summary>
        ///     At what HP this BodyPartis completely destroyed.
        /// </summary>
        [ViewVariables]
        public int DestroyThreshold { get; set; }

        /// <summary>
        ///     Armor of this BodyPart against attacks.
        /// </summary>
        [ViewVariables]
        public float Resistance { get; set; }

        /// <summary>
        ///     Determines many things: how many mechanisms can be fit inside this BodyPart, whether a body can fit through tiny crevices, etc.
        /// </summary>
        [ViewVariables]
        public int Size { get; set; }

        /// <summary>
        ///     What types of BodyParts this BodyPart can easily attach to. For the most part, most limbs aren't universal and require extra work to attach between types.
        /// </summary>
        [ViewVariables]
        public BodyPartCompatibility Compatibility { get; set; }

        /// <summary>
        ///     List of <see cref="IExposeData"/> properties, allowing for additional data classes to be attached to a limb, such as a "length" class to an arm.
        /// </summary>
        [ViewVariables]
        public List<IExposeData> Properties { get; set; }

        /// <summary>
        ///     List of all <see cref="Mechanism">Mechanisms</see> currently inside this BodyPart.
        /// </summary>
        [ViewVariables]
        public List<Mechanism.Mechanism> Mechanisms => _mechanisms;

        public BodyPart() { }

        public BodyPart(BodyPartPrototype data)
        {
            LoadFromPrototype(data);
        }






        public bool CanAttachBodyPart(BodyPart toBeConnected)
        {
            return _surgeryData.CanAttachBodyPart(toBeConnected);
        }






        /// <summary>
        ///     Returns whether the given <see cref="Mechanism"/> can be installed on this BodyPart.
        /// </summary>
        public bool CanInstallMechanism(Mechanism.Mechanism mechanism)
        {
            if (_sizeUsed + mechanism.Size > Size)
                return false; //No space
            return _surgeryData.CanInstallMechanism(mechanism);
        }

        /// <summary>
        ///     Attempts to add a <see cref="Mechanism"/>. Returns true if successful, false if there was an error (e.g. not enough room in BodyPart). Call InstallDroppedMechanism instead if you want to easily install an IEntity with a DroppedMechanismComponent.
        /// </summary>
        public bool TryInstallMechanism(Mechanism.Mechanism mechanism)
        {
            if (CanInstallMechanism(mechanism))
            {
                _mechanisms.Add(mechanism);
                _sizeUsed += mechanism.Size;
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Attempts to install a <see cref="DroppedMechanismComponent"/> into the given limb, potentially deleting the dropped <see cref="IEntity"/>. Returns true if successful, false if there was an error (e.g. not enough room in BodyPart).
        /// </summary>
        public bool TryInstallDroppedMechanism(DroppedMechanismComponent droppedMechanism)
        {
            if (!TryInstallMechanism(droppedMechanism.ContainedMechanism))
                return false; //Installing the mechanism failed for some reason.
            droppedMechanism.Owner.Delete();
            return true;
        }

        /// <summary>
        ///     Tries to remove the given <see cref="Mechanism"/> reference from this BodyPart. Returns null if there was an error in spawning the entity or removing the mechanism, otherwise returns a reference to the <see cref="DroppedMechanismComponent"/> on the newly spawned entity.
        /// </summary>
        public DroppedMechanismComponent DropMechanism(IEntity dropLocation, Mechanism.Mechanism mechanismTarget)
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
        ///     Tries to destroy the given <see cref="Mechanism"/> in the given BodyPart. Returns false if there was an error, true otherwise. Does NOT spawn a dropped entity.
        /// </summary>
        public bool DestroyMechanism(BodyPart bodyPartTarget, Mechanism.Mechanism mechanismTarget)
        {
            if (!_mechanisms.Contains(mechanismTarget))
                return false;
            _mechanisms.Remove(mechanismTarget);
            _sizeUsed -= mechanismTarget.Size;
            return true;
        }





        /// <summary>
        ///     Returns whether the given <see cref="SurgeryType"/> can be used on the current state of this BodyPart.
        /// </summary>
        public bool SurgeryCheck(SurgeryType toolType)
        {
            return _surgeryData.CheckSurgery(toolType);
        }

        /// <summary>
        ///     Attempts to perform surgery on this BodyPart with the given tool. Returns false if there was an error, true if successful.
        /// </summary>
        public bool AttemptSurgery(SurgeryType toolType, IBodyPartContainer target, ISurgeon surgeon, IEntity performer)
        {
            return _surgeryData.PerformSurgery(toolType, target, surgeon, performer);
        }





        /// <summary>
        ///    Loads the given <see cref="BodyPartPrototype"/> - current data on this <see cref="BodyPart"/> will be overwritten!
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
                _mechanisms.Add(new Mechanism.Mechanism(mechanismData));
            }

        }
    }
}
