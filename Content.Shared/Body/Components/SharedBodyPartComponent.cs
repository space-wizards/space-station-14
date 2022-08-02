using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems.Body;
using Content.Shared.Body.Systems.Part;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Body.Components
{
    [NetworkedComponent(), RegisterComponent]
    [Access(typeof(SharedBodyPartSystem), typeof(SharedBodySystem))]
    public sealed class SharedBodyPartComponent : Component
    {
        /// <summary>
        ///     A list of all mechanisms that should be added to this body part when spawned.
        /// </summary>
        [DataField("initialMechanisms", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
        public readonly List<string> InitialMechanisms = new();

        /// <summary>
        ///     Container that stores all the mechanism entities.
        /// </summary>
        [ViewVariables, NonSerialized]
        public Container? MechanismContainer = default!;

        [ViewVariables]
        public SharedBodyComponent? Body;

        /// <summary>
        ///     <see cref="BodyPartType"/> that this is considered
        ///     to be.
        ///     For example, <see cref="BodyPartType.Arm"/>.
        /// </summary>
        [ViewVariables]
        [DataField("partType")]
        public BodyPartType PartType = BodyPartType.Other;

        /// <summary>
        ///     Determines how many mechanisms can be fit inside this
        ///     <see cref="SharedBodyPartComponent"/>.
        /// </summary>
        [ViewVariables]
        [DataField("size")]
        public int Size = 1;

        /// <summary>
        ///     What types of BodyParts this <see cref="SharedBodyPartComponent"/> can easily attach to.
        ///     For the most part, most limbs aren't universal and require extra work to
        ///     attach between types. If no compatibility is set, the part is considered universal.
        /// </summary>
        [DataField("compatibility", customTypeSerializer: typeof(PrototypeIdSerializer<BodyPartCompatibilityPrototype>))]
        public string? Compatibility = null;

        [ViewVariables]
        [DataField("symmetry")]
        public BodyPartSymmetry Symmetry = BodyPartSymmetry.None;
    }

    [Serializable, NetSerializable]
    public sealed class BodyPartComponentState : ComponentState
    {
        public readonly BodyPartType PartType;
        public readonly BodyPartSymmetry Symmetry;

        public BodyPartComponentState(BodyPartType partType, BodyPartSymmetry symmetry)
        {
            PartType = partType;
            Symmetry = symmetry;
        }
    }
}
