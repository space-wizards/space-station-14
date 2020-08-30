#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Body.Mechanisms;
using Content.Server.Body.Surgery;
using Content.Server.GameObjects.Components.Body;
using Content.Server.GameObjects.Components.Metabolism;
using Content.Shared.Body.Mechanism;
using Content.Shared.Body.Part;
using Content.Shared.Body.Part.Properties;
using Content.Shared.Damage.DamageContainer;
using Content.Shared.Damage.ResistanceSet;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body
{
    /// <summary>
    ///     Data class representing a singular limb such as an arm or a leg.
    ///     Typically held within either a <see cref="BodyManagerComponent"/>,
    ///     which coordinates functions between BodyParts, or a
    ///     <see cref="DroppedBodyPartComponent"/>.
    /// </summary>
    public class BodyPart : IBodyPart
    {
        private IBodyManagerComponent? _body;

        private readonly HashSet<Mechanism> _mechanisms = new HashSet<Mechanism>();

        public BodyPart(BodyPartPrototype data)
        {
            SurgeryData = null!;
            Properties = new HashSet<IExposeData>();
            Name = null!;
            Plural = null!;
            RSIPath = null!;
            RSIState = null!;
            RSIMap = null!;
            Damage = null!;
            Resistances = null!;

            LoadFromPrototype(data);
        }

        [ViewVariables]
        public IBodyManagerComponent? Body
        {
            get => _body;
            set
            {
                var old = _body;
                _body = value;

                if (value == null && old != null)
                {
                    foreach (var mechanism in Mechanisms)
                    {
                        mechanism.RemovedFromBody(old);
                    }
                }
                else
                {
                    foreach (var mechanism in Mechanisms)
                    {
                        mechanism.InstalledIntoBody();
                    }
                }
            }
        }

        /// <summary>
        ///     The <see cref="Surgery.SurgeryData"/> class currently representing this BodyPart's
        ///     surgery status.
        /// </summary>
        [ViewVariables] private SurgeryData SurgeryData { get; set; }

        /// <summary>
        ///     How much space is currently taken up by Mechanisms in this BodyPart.
        /// </summary>
        [ViewVariables] private int SizeUsed { get; set; }

        /// <summary>
        ///     List of <see cref="IExposeData"/> properties, allowing for additional
        ///     data classes to be attached to a limb, such as a "length" class to an arm.
        /// </summary>
        [ViewVariables]
        private HashSet<IExposeData> Properties { get; }

        [ViewVariables] public string Name { get; private set; }

        [ViewVariables] public string Plural { get; private set; }

        [ViewVariables] public string RSIPath { get; private set; }

        [ViewVariables] public string RSIState { get; private set; }

        [ViewVariables] public Enum? RSIMap { get; set; }

        // TODO: SpriteComponent rework
        [ViewVariables] public Color? RSIColor { get; set; }

        [ViewVariables] public BodyPartType PartType { get; private set; }

        [ViewVariables] public int Size { get; private set; }

        [ViewVariables] public int MaxDurability { get; private set; }

        [ViewVariables] public int CurrentDurability => MaxDurability - Damage.TotalDamage;

        // TODO: Individual body part damage
        /// <summary>
        ///     Current damage dealt to this <see cref="IBodyPart"/>.
        /// </summary>
        [ViewVariables]
        public DamageContainer Damage { get; private set; }

        /// <summary>
        ///     Armor of this <see cref="IBodyPart"/> against damages.
        /// </summary>
        [ViewVariables]
        public ResistanceSet Resistances { get; private set; }

        /// <summary>
        ///     At what HP this <see cref="IBodyPart"/> destroyed.
        /// </summary>
        [ViewVariables]
        public int DestroyThreshold { get; private set; }

        /// <summary>
        ///     What types of BodyParts this <see cref="IBodyPart"/> can easily attach to.
        ///     For the most part, most limbs aren't universal and require extra work to
        ///     attach between types.
        /// </summary>
        [ViewVariables]
        public BodyPartCompatibility Compatibility { get; private set; }

        /// <summary>
        ///     Set of all <see cref="Mechanism"/> currently inside this
        ///     <see cref="IBodyPart"/>.
        /// </summary>
        [ViewVariables]
        public IReadOnlyCollection<Mechanism> Mechanisms => _mechanisms;

        /// <summary>
        ///     This method is called by
        ///     <see cref="IBodyManagerComponent.PreMetabolism"/> before
        ///     <see cref="MetabolismComponent.Update"/> is called.
        /// </summary>
        public void PreMetabolism(float frameTime)
        {
            foreach (var mechanism in Mechanisms)
            {
                mechanism.PreMetabolism(frameTime);
            }
        }

        /// <summary>
        ///     This method is called by
        ///     <see cref="IBodyManagerComponent.PostMetabolism"/> after
        ///     <see cref="MetabolismComponent.Update"/> is called.
        /// </summary>
        public void PostMetabolism(float frameTime)
        {
            foreach (var mechanism in Mechanisms)
            {
                mechanism.PreMetabolism(frameTime);
            }
        }

        /// <summary>
        ///     Attempts to add the given <see cref="BodyPartProperty"/>.
        /// </summary>
        /// <returns>
        ///     True if a <see cref="BodyPartProperty"/> of that type doesn't exist,
        ///     false otherwise.
        /// </returns>
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
        ///     Attempts to retrieve the given <see cref="BodyPartProperty"/> type.
        ///     The resulting <see cref="BodyPartProperty"/> will be null if unsuccessful.
        /// </summary>
        /// <param name="property">The property if found, null otherwise.</param>
        /// <typeparam name="T">The type of the property to find.</typeparam>
        /// <returns>True if successful, false otherwise.</returns>
        public bool TryGetProperty<T>([NotNullWhen(true)] out T? property) where T : BodyPartProperty
        {
            property = (T?) Properties.FirstOrDefault(x => x.GetType() == typeof(T));

            return property != null;
        }

        /// <summary>
        ///     Attempts to retrieve the given <see cref="BodyPartProperty"/> type.
        ///     The resulting <see cref="BodyPartProperty"/> will be null if unsuccessful.
        /// </summary>
        /// <returns>True if successful, false otherwise.</returns>
        public bool TryGetProperty(Type propertyType, [NotNullWhen(true)] out BodyPartProperty? property)
        {
            property = (BodyPartProperty?) Properties.First(x => x.GetType() == propertyType);

            return property != null;
        }

        /// <summary>
        ///     Checks if the given type <see cref="T"/> is on this <see cref="IBodyPart"/>.
        /// </summary>
        /// <typeparam name="T">
        ///     The subtype of <see cref="BodyPartProperty"/> to look for.
        /// </typeparam>
        /// <returns>
        ///     True if this <see cref="IBodyPart"/> has a property of type
        ///     <see cref="T"/>, false otherwise.
        /// </returns>
        public bool HasProperty<T>() where T : BodyPartProperty
        {
            return Properties.Count(x => x.GetType() == typeof(T)) > 0;
        }

        /// <summary>
        ///     Checks if a subtype of <see cref="BodyPartProperty"/> is on this
        ///     <see cref="IBodyPart"/>.
        /// </summary>
        /// <param name="propertyType">
        ///     The subtype of <see cref="BodyPartProperty"/> to look for.
        /// </param>
        /// <returns>
        ///     True if this <see cref="IBodyPart"/> has a property of type
        ///     <see cref="propertyType"/>, false otherwise.
        /// </returns>
        public bool HasProperty(Type propertyType)
        {
            return Properties.Count(x => x.GetType() == propertyType) > 0;
        }

        public bool CanAttachPart(IBodyPart part)
        {
            return SurgeryData.CanAttachBodyPart(part);
        }

        public bool CanInstallMechanism(Mechanism mechanism)
        {
            return SizeUsed + mechanism.Size <= Size &&
                   SurgeryData.CanInstallMechanism(mechanism);
        }

        /// <summary>
        ///     Tries to install a mechanism onto this body part.
        ///     Call <see cref="TryInstallDroppedMechanism"/> instead if you want to
        ///     easily install an <see cref="IEntity"/> with a
        ///     <see cref="DroppedMechanismComponent"/>.
        /// </summary>
        /// <param name="mechanism">The mechanism to try to install.</param>
        /// <returns>
        ///     True if successful, false if there was an error
        ///     (e.g. not enough room in <see cref="IBodyPart"/>).
        /// </returns>
        private bool TryInstallMechanism(Mechanism mechanism)
        {
            if (!CanInstallMechanism(mechanism))
            {
                return false;
            }

            AddMechanism(mechanism);

            return true;
        }

        /// <summary>
        ///     Tries to install a <see cref="DroppedMechanismComponent"/> into this
        ///     <see cref="IBodyPart"/>, potentially deleting the dropped
        ///     <see cref="IEntity"/>.
        /// </summary>
        /// <param name="droppedMechanism">The mechanism to install.</param>
        /// <returns>
        ///     True if successful, false if there was an error
        ///     (e.g. not enough room in <see cref="BodyPart"/>).
        /// </returns>
        public bool TryInstallDroppedMechanism(DroppedMechanismComponent droppedMechanism)
        {
            if (!TryInstallMechanism(droppedMechanism.ContainedMechanism))
            {
                return false; // Installing the mechanism failed for some reason.
            }

            droppedMechanism.Owner.Delete();
            return true;
        }

        public bool TryDropMechanism(IEntity dropLocation, Mechanism mechanismTarget,
            [NotNullWhen(true)] out DroppedMechanismComponent dropped)
        {
            dropped = null!;

            if (!_mechanisms.Remove(mechanismTarget))
            {
                return false;
            }

            SizeUsed -= mechanismTarget.Size;

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var position = dropLocation.Transform.GridPosition;
            var mechanismEntity = entityManager.SpawnEntity("BaseDroppedMechanism", position);

            dropped = mechanismEntity.GetComponent<DroppedMechanismComponent>();
            dropped.InitializeDroppedMechanism(mechanismTarget);

            return true;
        }

        /// <summary>
        ///     Tries to destroy the given <see cref="Mechanism"/> in this
        ///     <see cref="IBodyPart"/>. Does NOT spawn a dropped entity.
        /// </summary>
        /// <summary>
        ///     Tries to destroy the given <see cref="Mechanism"/> in this
        ///     <see cref="IBodyPart"/>.
        /// </summary>
        /// <param name="mechanismTarget">The mechanism to destroy.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public bool DestroyMechanism(Mechanism mechanismTarget)
        {
            if (!RemoveMechanism(mechanismTarget))
            {
                return false;
            }

            return true;
        }

        public bool SurgeryCheck(SurgeryType surgery)
        {
            return SurgeryData.CheckSurgery(surgery);
        }

        /// <summary>
        ///     Attempts to perform surgery on this <see cref="IBodyPart"/> with the given
        ///     tool.
        /// </summary>
        /// <returns>True if successful, false if there was an error.</returns>
        public bool AttemptSurgery(SurgeryType toolType, IBodyPartContainer target, ISurgeon surgeon, IEntity performer)
        {
            return SurgeryData.PerformSurgery(toolType, target, surgeon, performer);
        }

        private void AddMechanism(Mechanism mechanism)
        {
            DebugTools.AssertNotNull(mechanism);

            _mechanisms.Add(mechanism);
            SizeUsed += mechanism.Size;
            mechanism.Part = this;

            mechanism.EnsureInitialize();

            if (Body == null)
            {
                return;
            }

            if (!Body.Template.MechanismLayers.TryGetValue(mechanism.Id, out var mapString))
            {
                return;
            }

            if (!IoCManager.Resolve<IReflectionManager>().TryParseEnumReference(mapString, out var @enum))
            {
                Logger.Warning($"Template {Body.Template.Name} has an invalid RSI map key {mapString} for mechanism {mechanism.Id}.");
                return;
            }

            var message = new MechanismSpriteAddedMessage(@enum);

            Body.Owner.SendNetworkMessage(Body, message);
        }

        /// <summary>
        ///     Tries to remove the given <see cref="mechanism"/> from this
        ///     <see cref="IBodyPart"/>.
        /// </summary>
        /// <param name="mechanism">The mechanism to remove.</param>
        /// <returns>True if it was removed, false otherwise.</returns>
        private bool RemoveMechanism(Mechanism mechanism)
        {
            DebugTools.AssertNotNull(mechanism);

            if (!_mechanisms.Remove(mechanism))
            {
                return false;
            }

            SizeUsed -= mechanism.Size;
            mechanism.Part = null;

            if (Body == null)
            {
                return true;
            }

            if (!Body.Template.MechanismLayers.TryGetValue(mechanism.Id, out var mapString))
            {
                return true;
            }

            if (!IoCManager.Resolve<IReflectionManager>().TryParseEnumReference(mapString, out var @enum))
            {
                Logger.Warning($"Template {Body.Template.Name} has an invalid RSI map key {mapString} for mechanism {mechanism.Id}.");
                return true;
            }

            var message = new MechanismSpriteRemovedMessage(@enum);

            Body.Owner.SendNetworkMessage(Body, message);

            return true;
        }

        /// <summary>
        ///     Loads the given <see cref="BodyPartPrototype"/>.
        ///     Current data on this <see cref="IBodyPart"/> will be overwritten!
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

            if (!prototypeManager.TryIndex(data.DamageContainerPresetId,
                out DamageContainerPrototype damageContainerData))
            {
                throw new InvalidOperationException(
                    $"No {nameof(DamageContainerPrototype)} found with id {data.DamageContainerPresetId}");
            }

            Damage = new DamageContainer(OnHealthChanged, damageContainerData);

            if (!prototypeManager.TryIndex(data.ResistanceSetId, out ResistanceSetPrototype resistancesData))
            {
                throw new InvalidOperationException(
                    $"No {nameof(ResistanceSetPrototype)} found with id {data.ResistanceSetId}");
            }

            Resistances = new ResistanceSet(resistancesData);
            Size = data.Size;
            Compatibility = data.Compatibility;

            Properties.Clear();
            Properties.UnionWith(data.Properties);

            var surgeryDataType = Type.GetType(data.SurgeryDataName);

            if (surgeryDataType == null)
            {
                throw new InvalidOperationException($"No {nameof(Surgery.SurgeryData)} found with name {data.SurgeryDataName}");
            }

            if (!surgeryDataType.IsSubclassOf(typeof(SurgeryData)))
            {
                throw new InvalidOperationException(
                    $"Class {data.SurgeryDataName} is not a subtype of {nameof(Surgery.SurgeryData)} with id {data.ID}");
            }

            SurgeryData = IoCManager.Resolve<IDynamicTypeFactory>().CreateInstance<SurgeryData>(surgeryDataType, new object[] {this});

            foreach (var id in data.Mechanisms)
            {
                if (!prototypeManager.TryIndex(id, out MechanismPrototype mechanismData))
                {
                    throw new InvalidOperationException($"No {nameof(MechanismPrototype)} found with id {id}");
                }

                var mechanism = new Mechanism(mechanismData);

                AddMechanism(mechanism);
            }
        }

        private void OnHealthChanged(List<HealthChangeData> changes)
        {
            // TODO
        }

        public bool SpawnDropped([NotNullWhen(true)] out IEntity dropped)
        {
            dropped = default!;

            if (Body == null)
            {
                return false;
            }

            dropped = IoCManager.Resolve<IEntityManager>().SpawnEntity("BaseDroppedBodyPart", Body.Owner.Transform.GridPosition);

            dropped.GetComponent<DroppedBodyPartComponent>().TransferBodyPartData(this);

            return true;
        }
    }
}
