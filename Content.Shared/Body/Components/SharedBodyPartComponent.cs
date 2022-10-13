using Content.Shared.Body.Part;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Components
{
    [NetworkedComponent]
    public abstract class SharedBodyPartComponent : Component
    {
        [ViewVariables]
        [DataField("parent")]
        public EntityUid? Parent;

        [ViewVariables]
        [DataField("children")]
        public readonly HashSet<EntityUid> Children = new();

        [ViewVariables]
        [DataField("organs")]
        public readonly HashSet<EntityUid> Organs = new();

        [ViewVariables]
        [DataField("partType")]
        public BodyPartType PartType { get; private set; } = BodyPartType.Other;

        // TODO BODY Replace with a simulation of organs
        /// <summary>
        ///     Whether or not the owning <see cref="Body"/> will die if all
        ///     <see cref="SharedBodyPartComponent"/>s of this type are removed from it.
        /// </summary>
        [ViewVariables]
        [DataField("vital")]
        public bool IsVital;

        [ViewVariables]
        [DataField("symmetry")]
        public BodyPartSymmetry Symmetry = BodyPartSymmetry.None;
    }

    [Serializable, NetSerializable]
    public sealed class BodyPartComponentState : ComponentState
    {
        public readonly HashSet<EntityUid> Organs;

        public BodyPartComponentState(HashSet<EntityUid> organs)
        {
            Organs = organs;
        }
    }
}
