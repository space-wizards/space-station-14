#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Body.Mechanisms;
using Content.Server.GameObjects.Components.Body;
using Content.Shared.Body.Part.Properties;
using Content.Shared.GameObjects.Components.Body;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.Body
{
    public interface IBodyPart
    {
        /// <summary>
        ///     The body that this body part is currently in, if any.
        /// </summary>
        IBodyManagerComponent? Body { get; set; }

        /// <summary>
        ///     <see cref="BodyPartType"/> that this <see cref="IBodyPart"/> is considered
        ///     to be.
        ///     For example, <see cref="BodyPartType.Arm"/>.
        /// </summary>
        BodyPartType PartType { get; }

        /// <summary>
        ///     The name of this <see cref="IBodyPart"/>, often displayed to the user.
        ///     For example, it could be named "advanced robotic arm".
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Plural version of this <see cref="IBodyPart"/> name.
        /// </summary>
        public string Plural { get; }

        /// <summary>
        ///     Determines many things: how many mechanisms can be fit inside this
        ///     <see cref="IBodyPart"/>, whether a body can fit through tiny crevices,
        ///     etc.
        /// </summary>
        int Size { get; }

        /// <summary>
        ///     Max HP of this <see cref="IBodyPart"/>.
        /// </summary>
        int MaxDurability { get; }

        /// <summary>
        ///     Current HP of this <see cref="IBodyPart"/> based on sum of all damage types.
        /// </summary>
        int CurrentDurability { get; }

        /// <summary>
        ///     Collection of all <see cref="Mechanism"/>s currently inside this
        ///     <see cref="IBodyPart"/>.
        ///     To add and remove from this list see <see cref="AddMechanism"/> and
        ///     <see cref="RemoveMechanism"/>
        /// </summary>
        IReadOnlyCollection<Mechanism> Mechanisms { get; }

        /// <summary>
        ///     Path to the RSI that represents this <see cref="IBodyPart"/>.
        /// </summary>
        public string RSIPath { get; }

        /// <summary>
        ///     RSI state that represents this <see cref="IBodyPart"/>.
        /// </summary>
        public string RSIState { get; }

        /// <summary>
        ///     RSI map keys that this body part changes on the sprite.
        /// </summary>
        public Enum? RSIMap { get; set; }

        /// <summary>
        ///     RSI color of this body part.
        /// </summary>
        // TODO: SpriteComponent rework
        public Color? RSIColor { get; set; }

        bool HasProperty<T>() where T : BodyPartProperty;

        bool HasProperty(Type type);

        bool TryGetProperty<T>([NotNullWhen(true)] out T? property) where T : BodyPartProperty;

        void PreMetabolism(float frameTime);

        void PostMetabolism(float frameTime);

        bool SpawnDropped([NotNullWhen(true)] out IEntity? dropped);

        /// <summary>
        ///     Checks if the given <see cref="SurgeryType"/> can be used on
        ///     the current state of this <see cref="IBodyPart"/>.
        /// </summary>
        /// <returns>True if it can be used, false otherwise.</returns>
        bool SurgeryCheck(SurgeryType surgery);

        /// <summary>
        ///     Checks if another <see cref="IBodyPart"/> can be connected to this one.
        /// </summary>
        /// <param name="part">The part to connect.</param>
        /// <returns>True if it can be connected, false otherwise.</returns>
        bool CanAttachPart(IBodyPart part);

        /// <summary>
        ///     Checks if a <see cref="Mechanism"/> can be installed on this
        ///     <see cref="IBodyPart"/>.
        /// </summary>
        /// <returns>True if it can be installed, false otherwise.</returns>
        bool CanInstallMechanism(Mechanism mechanism);

        /// <summary>
        ///     Tries to remove the given <see cref="Mechanism"/> reference from
        ///     this <see cref="IBodyPart"/>.
        /// </summary>
        /// <returns>
        ///     The newly spawned <see cref="DroppedMechanismComponent"/>, or null
        ///     if there was an error in spawning the entity or removing the mechanism.
        /// </returns>
        bool TryDropMechanism(IEntity dropLocation, Mechanism mechanismTarget,
            [NotNullWhen(true)] out DroppedMechanismComponent dropped);

        bool DestroyMechanism(Mechanism mechanism);
    }
}
