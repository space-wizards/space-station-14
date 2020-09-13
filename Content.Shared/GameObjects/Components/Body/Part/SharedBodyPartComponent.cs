#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body.Part.Properties;
using Content.Shared.Damage.DamageContainer;
using Content.Shared.Damage.ResistanceSet;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Surgery;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Body.Part
{
    public abstract class SharedBodyPartComponent : Component, IBodyPart, IBodyPartContainer
    {
        public override string Name => "BodyPart";

        private DamageContainerPrototype _damagePrototype = default!;
        private ResistanceSetPrototype _resistancePrototype = default!;
        // TODO Serialize
        private HashSet<IMechanism> _mechanisms = new HashSet<IMechanism>();

        [ViewVariables] public IBody? Body { get; set; }

        [ViewVariables] public BodyPartType PartType { get; private set; }

        [ViewVariables] public string Plural { get; private set; } = string.Empty;

        [ViewVariables] public int Size { get; private set; }

        [ViewVariables] public int SizeUsed { get; private set; }

        [ViewVariables] public int MaxDurability { get; private set; }

        // TODO MaxDurability - Damage.TotalDamage;
        // TODO Individual body part damage
        [ViewVariables] public int CurrentDurability { get; private set; }

        // TODO size used
        // TODO surgerydata
        // TODO properties

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
        ///     Set of all <see cref="IMechanism"/> currently inside this
        ///     <see cref="IBodyPart"/>.
        /// </summary>
        [ViewVariables]
        public IReadOnlyCollection<IMechanism> Mechanisms => _mechanisms;

        [ViewVariables] public string RSIPath { get; private set; } = string.Empty;

        [ViewVariables] public string RSIState { get; private set; } = string.Empty;

        [ViewVariables] public Enum? RSIMap { get; set; }

        [ViewVariables] public Color? RSIColor { get; set; }

        // TODO Replace with a simulation of organs
        /// <summary>
        ///     Represents if body part is vital for creature.
        ///     If the last vital body part is removed creature dies
        /// </summary>
        [ViewVariables]
        public bool IsVital { get; private set; }

        // TODO
        /// <summary>
        ///     Current damage dealt to this <see cref="IBodyPart"/>.
        /// </summary>
        [ViewVariables]
        public DamageContainer Damage { get; private set; } = default!;

        // TODO
        /// <summary>
        ///     Armor of this <see cref="IBodyPart"/> against damage.
        /// </summary>
        [ViewVariables]
        public ResistanceSet Resistances { get; private set; } = default!;

        // TODO
        [ViewVariables] public SurgeryData SurgeryData { get; private set; } = default!;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            // TODO BODY Separate damage from the rest of body system
            serializer.DataReadWriteFunction(
                "damagePrototype",
                null,
                prototype =>
                {
                    _damagePrototype = prototype ?? throw new NullReferenceException();
                    Damage = new DamageContainer(OnHealthChanged, _damagePrototype);
                },
                () => _damagePrototype);

            serializer.DataReadWriteFunction(
                "resistancePrototype",
                null,
                prototype =>
                {
                    _resistancePrototype = prototype ?? throw new NullReferenceException();
                    Resistances = new ResistanceSet(_resistancePrototype);
                },
                () => _resistancePrototype);

            serializer.DataField(this, b => b.PartType, "partType", BodyPartType.Other);

            serializer.DataField(this, b => b.Plural, "plural", string.Empty);

            serializer.DataField(this, b => b.Size, "size", 1);

            serializer.DataField(this, b => b.MaxDurability, "maxDurability", 10);

            serializer.DataField(this, b => b.CurrentDurability, "currentDurability", MaxDurability);

            serializer.DataField(this, m => m.RSIPath, "rsiPath", string.Empty);

            serializer.DataField(this, m => m.RSIState, "rsiState", string.Empty);

            serializer.DataField(this, m => m.RSIMap, "rsiMap", null);

            serializer.DataField(this, m => m.RSIColor, "rsiColor", null);

            serializer.DataField(this, m => m.IsVital, "vital", false);
        }

        public bool Drop()
        {
            var grandParent = Owner.Transform.Parent?.Parent;

            if (grandParent?.Parent == null)
            {
                Owner.Transform.AttachToGridOrMap();
                return true;
            }

            Owner.Transform.AttachParent(grandParent.Parent);
            return true;
        }

        public bool SurgeryCheck(SurgeryType surgery)
        {
            return SurgeryData.CheckSurgery(surgery);
        }

        public bool CanAttachPart(IBodyPart part)
        {
            return SurgeryData.CanAttachBodyPart(part);
        }

        public bool CanInstallMechanism(IMechanism mechanism)
        {
            return SizeUsed + mechanism.Size <= Size &&
                   SurgeryData.CanInstallMechanism(mechanism);
        }

        private bool AddMechanism(IMechanism mechanism)
        {
            return _mechanisms.Add(mechanism);
        }

        /// <summary>
        ///     Tries to install a mechanism onto this body part.
        /// </summary>
        /// <param name="mechanism">The mechanism to try to install.</param>
        /// <returns>
        ///     True if successful, false if there was an error
        ///     (e.g. not enough room in <see cref="IBodyPart"/>).
        /// </returns>
        private bool TryInstallMechanism(IMechanism mechanism)
        {
            if (!CanInstallMechanism(mechanism))
            {
                return false;
            }

            return AddMechanism(mechanism);
        }

        public bool TryDropMechanism(IEntity dropLocation, IMechanism mechanismTarget)
        {
            if (!_mechanisms.Remove(mechanismTarget))
            {
                return false;
            }

            SizeUsed -= mechanismTarget.Size;

            return true;
        }

        public abstract bool DestroyMechanism(IMechanism mechanism);

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
        ///     Checks if the given type <see cref="T"/> is on this
        ///     <see cref="IBodyPart"/>.
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

        private void OnHealthChanged(List<HealthChangeData> changes)
        {
            // TODO
        }
    }
}
