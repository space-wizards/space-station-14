#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.Damage.DamageContainer;
using Content.Shared.Damage.ResistanceSet;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Surgery;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
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

        /// <summary>
        ///     Attempts to perform surgery on this <see cref="IBodyPart"/> with the given
        ///     tool.
        /// </summary>
        /// <returns>True if successful, false if there was an error.</returns>
        public bool AttemptSurgery(SurgeryType toolType, IBodyPartContainer target, ISurgeon surgeon, IEntity performer)
        {
            DebugTools.AssertNotNull(toolType);
            DebugTools.AssertNotNull(target);
            DebugTools.AssertNotNull(surgeon);
            DebugTools.AssertNotNull(performer);

            return SurgeryData.PerformSurgery(toolType, target, surgeon, performer);
        }

        public bool CanAttachPart(IBodyPart part)
        {
            DebugTools.AssertNotNull(part);

            return SurgeryData.CanAttachBodyPart(part);
        }

        public bool CanInstallMechanism(IMechanism mechanism)
        {
            DebugTools.AssertNotNull(mechanism);

            return SizeUsed + mechanism.Size <= Size &&
                   SurgeryData.CanInstallMechanism(mechanism);
        }

        /// <summary>
        ///     Tries to install a mechanism onto this body part.
        /// </summary>
        /// <param name="mechanism">The mechanism to try to install.</param>
        /// <returns>
        ///     True if successful, false if there was an error
        ///     (e.g. not enough room in <see cref="IBodyPart"/>).
        /// </returns>
        public bool TryInstallMechanism(IMechanism mechanism)
        {
            DebugTools.AssertNotNull(mechanism);

            if (!CanInstallMechanism(mechanism))
            {
                return false;
            }

            if (_mechanisms.Add(mechanism))
            {
                mechanism.Part = this;
                SizeUsed += mechanism.Size;

                if (Body == null)
                {
                    return true;
                }

                if (!Body.MechanismLayers.TryGetValue(mechanism.Id, out var mapString))
                {
                    return true;
                }

                if (!IoCManager.Resolve<IReflectionManager>().TryParseEnumReference(mapString, out var @enum))
                {
                    Logger.Warning($"Template {Body.TemplateName} has an invalid RSI map key {mapString} for mechanism {mechanism.Id}.");
                    return true;
                }

                var message = new MechanismSpriteAddedMessage(@enum);

                Body.Owner.SendNetworkMessage(Body, message);

                return true;
            }

            return false;
        }

        public bool RemoveMechanism(IMechanism mechanism)
        {
            DebugTools.AssertNotNull(mechanism);

            if (!_mechanisms.Remove(mechanism))
            {
                return false;
            }

            mechanism.Part = null;
            SizeUsed -= mechanism.Size;

            if (Body == null)
            {
                return true;
            }

            if (!Body.MechanismLayers.TryGetValue(mechanism.Id, out var mapString))
            {
                return true;
            }

            if (!IoCManager.Resolve<IReflectionManager>().TryParseEnumReference(mapString, out var @enum))
            {
                Logger.Warning($"Template {Body.TemplateName} has an invalid RSI map key {mapString} for mechanism {mechanism.Id}.");
                return true;
            }

            var message = new MechanismSpriteRemovedMessage(@enum);

            Body.Owner.SendNetworkMessage(Body, message);

            return true;
        }

        public bool RemoveMechanism(IMechanism mechanism, EntityCoordinates coordinates)
        {
            if (RemoveMechanism(mechanism))
            {
                mechanism.Owner.Transform.Coordinates = coordinates;
                return true;
            }

            return false;
        }

        public bool DeleteMechanism(IMechanism mechanism)
        {
            DebugTools.AssertNotNull(mechanism);

            if (!RemoveMechanism(mechanism))
            {
                return false;
            }

            mechanism.Owner.Delete();
            return true;
        }

        private void OnHealthChanged(List<HealthChangeData> changes)
        {
            // TODO
        }
    }

    /// <summary>
    ///     Used to determine whether a BodyPart can connect to another BodyPart.
    /// </summary>
    [Serializable, NetSerializable]
    public enum BodyPartCompatibility
    {
        Universal = 0,
        Biological,
        Mechanical
    }

    /// <summary>
    ///     Each BodyPart has a BodyPartType used to determine a variety of things.
    ///     For instance, what slots it can fit into.
    /// </summary>
    [Serializable, NetSerializable]
    public enum BodyPartType
    {
        Other = 0,
        Torso,
        Head,
        Arm,
        Hand,
        Leg,
        Foot
    }
}
