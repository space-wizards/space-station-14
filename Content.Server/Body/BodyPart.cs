using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Body.Mechanisms;
using Content.Server.Body.Surgery;
using Content.Server.GameObjects.Components.Body;
using Content.Shared.BodySystem;
using Content.Shared.DamageSystem;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body
{
    /// <summary>
    ///     Data class representing a singular limb such as an arm or a leg. Typically held within either a
    ///     <see cref="BodyManagerComponent"/>,
    ///     which coordinates functions between BodyParts, or a <see cref="DroppedBodyPartComponent"/>.
    /// </summary>
    public class BodyPart
    {
        /// <summary>
        ///     List of <see cref="Mechanism">Mechanisms</see> currently held within this BodyPart.
        /// </summary>
        [ViewVariables] private readonly List<Mechanism> _mechanisms = new List<Mechanism>();

        /// <summary>
        ///     How much space is currently taken up by Mechanisms in this BodyPart.
        /// </summary>
        [ViewVariables] private int _sizeUsed;

        /// <summary>
        ///     The <see cref="SurgeryData"/> class currently representing this BodyPart's surgery status.
        /// </summary>
        [ViewVariables] private SurgeryData _surgeryData;

        public BodyPart() { }

        public BodyPart(BodyPartPrototype data)
        {
            LoadFromPrototype(data);
        }

        /// <summary>
        ///     List of <see cref="IExposeData"/> properties, allowing for additional data classes to be attached to a limb, such
        ///     as a "length" class to an arm.
        /// </summary>
        [ViewVariables]
        private List<IExposeData> Properties { get; set; }

        /// <summary>
        ///     The name of this BodyPart, often displayed to the user. For example, it could be named "advanced robotic arm".
        /// </summary>
        [ViewVariables]
        public string Name { get; private set; }

        /// <summary>
        ///     Plural version of this BodyPart name.
        /// </summary>
        [ViewVariables]
        public string Plural { get; private set; }

        /// <summary>
        ///     Path to the RSI that represents this BodyPart.
        /// </summary>
        [ViewVariables]
        public string RSIPath { get; private set; }

        /// <summary>
        ///     RSI state that represents this BodyPart.
        /// </summary>
        [ViewVariables]
        public string RSIState { get; private set; }

        /// <summary>
        ///     <see cref="BodyPartType"/> that this BodyPart is considered to be. For example, BodyPartType.Arm.
        /// </summary>
        [ViewVariables]
        public BodyPartType PartType { get; private set; }

        /// <summary>
        ///     Determines many things: how many mechanisms can be fit inside this BodyPart, whether a body can fit through tiny
        ///     crevices, etc.
        /// </summary>
        [ViewVariables]
        private int Size { get; set; }

        /// <summary>
        ///     Max HP of this BodyPart.
        /// </summary>
        [ViewVariables]
        public int MaxDurability { get; private set; }

        /// <summary>
        ///     Current HP of this BodyPart based on sum of all damage types.
        /// </summary>
        [ViewVariables]
        public int CurrentDurability => MaxDurability - CurrentDamages.TotalDamage;

        /// <summary>
        ///     Current damage dealt to this BodyPart.
        /// </summary>
        [ViewVariables]
        public DamageContainer CurrentDamages { get; private set; }

        /// <summary>
        ///     Armor of this BodyPart against damages.
        /// </summary>
        [ViewVariables]
        public ResistanceSet Resistances { get; private set; }

        /// <summary>
        ///     At what HP this BodyPart destroyed.
        /// </summary>
        [ViewVariables]
        public int DestroyThreshold { get; private set; }

        /// <summary>
        ///     What types of BodyParts this BodyPart can easily attach to. For the most part, most limbs aren't universal and
        ///     require extra work to attach between types.
        /// </summary>
        [ViewVariables]
        public BodyPartCompatibility Compatibility { get; private set; }

        /// <summary>
        ///     List of all <see cref="Mechanism">Mechanisms</see> currently inside this BodyPart.
        /// </summary>
        [ViewVariables]
        public List<Mechanism> Mechanisms => _mechanisms;

        /// <summary>
        ///     This function is called by <see cref="BodyManagerComponent"/>'s Tick function, which is called by
        ///     <see cref="BodySystem"/> every tick.
        /// </summary>
        public void Tick(float frameTime)
        {
            foreach (var mechanism in Mechanisms)
            {
                mechanism.Tick(frameTime);
            }
        }

        /// <summary>
        ///     Attempts to add the given <see cref="BodyPartProperty"/>. Returns true if successful, false if a BodyPartProperty
        ///     of that type already exists.
        /// </summary>
        public bool TryAddProperty(BodyPartProperty property)
        {
            if (HasProperty(property.GetType()))
            {
                return false;
            }

            Properties.Add(property);
            return true;
        }

        /// <summary>
        ///     Attempts to retrieve the given <see cref="BodyPartProperty"/> type. Returns true if successful, false otherwise.
        ///     The resulting BodyPartProperty will be null if unsuccessful.
        /// </summary>
        public bool TryGetProperty<T>(out T property)
        {
            property = (T) Properties.First(x => x.GetType() == typeof(T));
            return property != null;
        }

        /// <summary>
        ///     Attempts to retrieve the given <see cref="BodyPartProperty"/> type. Returns true if successful, false otherwise.
        ///     The resulting BodyPartProperty will be null if unsuccessful.
        /// </summary>
        public bool TryGetProperty(Type propertyType, out BodyPartProperty property)
        {
            property = (BodyPartProperty) Properties.First(x => x.GetType() == propertyType);
            return property != null;
        }

        /// <summary>
        ///     Returns whether the given <see cref="BodyPartProperty"/> type is on this BodyPart.
        /// </summary>
        public bool HasProperty<T>()
        {
            return Properties.Count(x => x.GetType() == typeof(T)) > 0;
        }

        /// <summary>
        ///     Returns whether the given <see cref="BodyPartProperty"/> type is on this BodyPart.
        /// </summary>
        public bool HasProperty(Type propertyType)
        {
            return Properties.Count(x => x.GetType() == propertyType) > 0;
        }


        public bool CanAttachBodyPart(BodyPart toBeConnected)
        {
            return _surgeryData.CanAttachBodyPart(toBeConnected);
        }

        /// <summary>
        ///     Returns whether the given <see cref="Mechanism"/> can be installed on this BodyPart.
        /// </summary>
        public bool CanInstallMechanism(Mechanism mechanism)
        {
            if (_sizeUsed + mechanism.Size > Size)
            {
                return false; //No space
            }

            return _surgeryData.CanInstallMechanism(mechanism);
        }

        /// <summary>
        ///     Attempts to add a <see cref="Mechanism"/>. Returns true if successful, false if there was an error (e.g. not
        ///     enough room in BodyPart). Call InstallDroppedMechanism instead if you want to easily install an IEntity with a
        ///     DroppedMechanismComponent.
        /// </summary>
        private bool TryInstallMechanism(Mechanism mechanism)
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
        ///     Attempts to install a <see cref="DroppedMechanismComponent"/> into the given limb, potentially deleting the
        ///     dropped <see cref="IEntity"/>. Returns true if successful, false if there was an error (e.g. not enough room in
        ///     BodyPart).
        /// </summary>
        public bool TryInstallDroppedMechanism(DroppedMechanismComponent droppedMechanism)
        {
            if (!TryInstallMechanism(droppedMechanism.ContainedMechanism))
            {
                return false; //Installing the mechanism failed for some reason.
            }

            droppedMechanism.Owner.Delete();
            return true;
        }

        /// <summary>
        ///     Tries to remove the given <see cref="Mechanism"/> reference from this BodyPart. Returns null if there was an error
        ///     in spawning the entity or removing the mechanism, otherwise returns a reference to the
        ///     <see cref="DroppedMechanismComponent"/> on the newly spawned entity.
        /// </summary>
        public DroppedMechanismComponent DropMechanism(IEntity dropLocation, Mechanism mechanismTarget)
        {
            if (!_mechanisms.Contains(mechanismTarget))
            {
                return null;
            }

            _mechanisms.Remove(mechanismTarget);
            _sizeUsed -= mechanismTarget.Size;
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mechanismEntity =
                entityManager.SpawnEntity("BaseDroppedMechanism", dropLocation.Transform.GridPosition);
            var droppedMechanism = mechanismEntity.GetComponent<DroppedMechanismComponent>();
            droppedMechanism.InitializeDroppedMechanism(mechanismTarget);
            return droppedMechanism;
        }

        /// <summary>
        ///     Tries to destroy the given <see cref="Mechanism"/> in the given BodyPart. Returns false if there was an error,
        ///     true otherwise. Does NOT spawn a dropped entity.
        /// </summary>
        public bool DestroyMechanism(BodyPart bodyPartTarget, Mechanism mechanismTarget)
        {
            if (!_mechanisms.Contains(mechanismTarget))
            {
                return false;
            }

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
        ///     Attempts to perform surgery on this BodyPart with the given tool. Returns false if there was an error, true if
        ///     successful.
        /// </summary>
        public bool AttemptSurgery(SurgeryType toolType, IBodyPartContainer target, ISurgeon surgeon, IEntity performer)
        {
            return _surgeryData.PerformSurgery(toolType, target, surgeon, performer);
        }

        /// <summary>
        ///     Loads the given <see cref="BodyPartPrototype"/> - current data on this <see cref="BodyPart"/> will be
        ///     overwritten!
        /// </summary>
        protected virtual void LoadFromPrototype(BodyPartPrototype data)
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            Name = data.Name;
            Plural = data.Plural;
            PartType = data.PartType;
            RSIPath = data.RSIPath;
            RSIState = data.RSIState;
            MaxDurability = data.Durability;
            if (!prototypeManager.TryIndex(data.DamageContainerPresetID,
                out DamageContainerPrototype damageContainerData))
            {
                throw new InvalidOperationException("No DamageContainerPrototype was found with the name " +
                                                    data.DamageContainerPresetID + "!");
            }

            CurrentDamages = new DamageContainer(damageContainerData);
            if (!prototypeManager.TryIndex(data.ResistanceSetID, out ResistanceSetPrototype resistancesData))
            {
                throw new InvalidOperationException("No ResistanceSetPrototype was found with the name " +
                                                    data.ResistanceSetID + "!");
            }

            Resistances = new ResistanceSet(resistancesData);
            Size = data.Size;
            Compatibility = data.Compatibility;
            Properties = data.Properties;
            var surgeryDataType = Type.GetType(data.SurgeryDataName);
            if (surgeryDataType == null)
            {
                throw new InvalidOperationException("No SurgeryData was found with the name " + data.SurgeryDataName +
                                                    "!");
            }

            if (!surgeryDataType.IsSubclassOf(typeof(SurgeryData)))
            {
                throw new InvalidOperationException("Class " + data.SurgeryDataName +
                                                    " is not a subtype of SurgeryData, but was provided as a SurgeryData for BodyPart prototype " +
                                                    data.ID + "!");
            }

            _surgeryData = (SurgeryData) Activator.CreateInstance(surgeryDataType, this);
            foreach (var id in data.Mechanisms)
            {
                if (!prototypeManager.TryIndex(id, out MechanismPrototype mechanismData))
                {
                    throw new InvalidOperationException($"No {nameof(MechanismPrototype)} found with name {id}");
                }

                _mechanisms.Add(new Mechanism(mechanismData));
            }
        }
    }
}
