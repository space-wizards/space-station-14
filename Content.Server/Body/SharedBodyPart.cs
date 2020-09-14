#nullable enable
using System;
using Content.Server.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Body.Surgery;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Content.Server.Body
{
    // TODO Remove
    /// <summary>
    ///     Data class representing a singular limb such as an arm or a leg.
    ///     Typically held within either a <see cref="BodyComponent"/>,
    ///     which coordinates functions between BodyParts, or a
    ///     <see cref="DroppedBodyPartComponent"/>.
    /// </summary>
    public class BodyPart : IBodyPart
    {
        /// <summary>
        ///     Tries to destroy the given <see cref="IMechanism"/> in this
        ///     <see cref="IBodyPart"/>. Does NOT spawn a dropped entity.
        /// </summary>
        /// <summary>
        ///     Tries to destroy the given <see cref="IMechanism"/> in this
        ///     <see cref="IBodyPart"/>.
        /// </summary>
        /// <param name="mechanismTarget">The mechanism to destroy.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public bool DestroyMechanism(IMechanism mechanismTarget)
        {
            if (!RemoveMechanism(mechanismTarget))
            {
                return false;
            }

            return true;
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

        private void AddMechanism(IMechanism mechanism)
        {
            DebugTools.AssertNotNull(mechanism);

            _mechanisms.Add(mechanism);
            SizeUsed += mechanism.Size;
            mechanism.Part = this;

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
        private bool RemoveMechanism(IMechanism mechanism)
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
            var surgeryDataType = Type.GetType(data.SurgeryDataName);

            if (surgeryDataType == null)
            {
                throw new InvalidOperationException($"No {nameof(SurgeryData)} found with name {data.SurgeryDataName}");
            }

            if (!surgeryDataType.IsSubclassOf(typeof(SurgeryData)))
            {
                throw new InvalidOperationException(
                    $"Class {data.SurgeryDataName} is not a subtype of {nameof(SurgeryData)} with id {data.ID}");
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

    }
}
