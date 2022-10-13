using System.Linq;
using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Body.Components
{
    [NetworkedComponent()]
    public abstract class SharedBodyPartComponent : Component
    {
        public const string ContainerId = "bodypart";

        [Dependency] private readonly IEntityManager _entMan = default!;
        private SharedBodyComponent? _body;

        [ViewVariables]
        private readonly HashSet<OrganComponent> _mechanisms = new();

        [ViewVariables]
        public SharedBodyComponent? Body
        {
            get => _body;
            set
            {
                if (_body == value)
                {
                    return;
                }

                var old = _body;
                _body = value;

                if (old != null)
                {
                    RemovedFromBody(old);
                }

                if (value != null)
                {
                    AddedToBody(value);
                }
            }
        }

        /// <summary>
        ///     <see cref="BodyPartType"/> that this <see cref="IBodyPart"/> is considered
        ///     to be.
        ///     For example, <see cref="BodyPartType.Arm"/>.
        /// </summary>
        [ViewVariables]
        [DataField("partType")]
        public BodyPartType PartType { get; private set; } = BodyPartType.Other;

        /// <summary>
        ///     Determines how many mechanisms can be fit inside this
        ///     <see cref="SharedBodyPartComponent"/>.
        /// </summary>
        [ViewVariables] [DataField("size")] public int Size { get; private set; } = 1;

        [ViewVariables] public int SizeUsed { get; private set; }

        // TODO BODY size used
        // TODO BODY surgerydata

        /// <summary>
        ///     What types of BodyParts this <see cref="SharedBodyPartComponent"/> can easily attach to.
        ///     For the most part, most limbs aren't universal and require extra work to
        ///     attach between types.
        /// </summary>
        [ViewVariables]
        [DataField("compatibility")]
        public BodyPartCompatibility Compatibility = BodyPartCompatibility.Universal;

        // TODO BODY Mechanisms occupying different parts at the body level
        [ViewVariables]
        public IReadOnlyCollection<OrganComponent> Mechanisms => _mechanisms;

        // TODO BODY Replace with a simulation of organs
        /// <summary>
        ///     Whether or not the owning <see cref="Body"/> will die if all
        ///     <see cref="SharedBodyPartComponent"/>s of this type are removed from it.
        /// </summary>
        [ViewVariables]
        [DataField("vital")]
        public bool IsVital = false;

        [ViewVariables]
        [DataField("symmetry")]
        public BodyPartSymmetry Symmetry = BodyPartSymmetry.None;

        protected virtual void OnAddMechanism(OrganComponent organ)
        {
            organ.Part = this;
            SizeUsed += organ.Size;

            Dirty();
        }

        protected virtual void OnRemoveMechanism(OrganComponent organ)
        {
            organ.Part = null;
            SizeUsed -= organ.Size;

            Dirty();
        }

        public override ComponentState GetComponentState()
        {
            var mechanismIds = new EntityUid[_mechanisms.Count];

            var i = 0;
            foreach (var mechanism in _mechanisms)
            {
                mechanismIds[i] = mechanism.Owner;
                i++;
            }

            return new BodyPartComponentState(mechanismIds);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not BodyPartComponentState state)
            {
                return;
            }

            var newMechanisms = state.Mechanisms();

            foreach (var mechanism in _mechanisms.ToArray())
            {
                if (!newMechanisms.Contains(mechanism))
                {
                    RemoveMechanism(mechanism);
                }
            }

            foreach (var mechanism in newMechanisms)
            {
                if (!_mechanisms.Contains(mechanism))
                {
                    TryAddMechanism(mechanism, true);
                }
            }
        }

        public virtual bool CanAddMechanism(OrganComponent organ)
        {
            return SizeUsed + organ.Size <= Size;
        }

        /// <summary>
        ///     Tries to add a <see cref="OrganComponent"/> to this part.
        /// </summary>
        /// <param name="organ">The organ to add.</param>
        /// <param name="force">
        ///     Whether or not to check if the organ is compatible.
        ///     Passing true does not guarantee it to be added, for example if
        ///     it was already added before.
        /// </param>
        /// <returns>true if added, false otherwise even if it was already added.</returns>
        public bool TryAddMechanism(OrganComponent organ, bool force = false)
        {
            DebugTools.AssertNotNull(organ);

            if (!force && !CanAddMechanism(organ))
            {
                return false;
            }

            if (!_mechanisms.Add(organ))
            {
                return false;
            }

            OnAddMechanism(organ);

            return true;
        }

        /// <summary>
        ///     Tries to remove the given <see cref="mechanism"/> from this part.
        /// </summary>
        /// <param name="organnism">The organ to remove.</param>
        /// <returns>True if it was removed, false otherwise.</returns>
        public bool RemoveMechanism(OrganComponent organ)
        {
            DebugTools.AssertNotNull(organ);

            if (!_mechanisms.Remove(organ))
            {
                return false;
            }

            OnRemoveMechanism(organ);

            return true;
        }

        /// <summary>
        ///     Tries to remove the given <see cref="mechanism"/> from this
        ///     part and drops it at the specified coordinates.
        /// </summary>
        /// <param name="organnism">The organ to remove.</param>
        /// <param name="coordinates">The coordinates to drop it at.</param>
        /// <returns>True if it was removed, false otherwise.</returns>
        public bool RemoveMechanism(OrganComponent organ, EntityCoordinates coordinates)
        {
            if (RemoveMechanism(organ))
            {
                _entMan.GetComponent<TransformComponent>(organ.Owner).Coordinates = coordinates;
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Tries to destroy the given <see cref="OrganComponent"/> from
        ///     this part.
        ///     The organ won't be deleted if it is not in this body part.
        /// </summary>
        /// <returns>
        ///     True if the organ was in this body part and destroyed,
        ///     false otherwise.
        /// </returns>
        public bool DeleteMechanism(OrganComponent organ)
        {
            DebugTools.AssertNotNull(organ);

            if (!RemoveMechanism(organ))
            {
                return false;
            }

            _entMan.DeleteEntity(organ.Owner);
            return true;
        }

        private void AddedToBody(SharedBodyComponent body)
        {
            OnAddedToBody(body);

            foreach (var mechanism in _mechanisms)
            {
                _entMan.EventBus.RaiseLocalEvent(mechanism.Owner, new AddedToBodyEvent(body), true);
            }
        }

        private void RemovedFromBody(SharedBodyComponent old)
        {
            if (_entMan.TryGetComponent<TransformComponent>(Owner, out var transformComponent))
            {
                transformComponent.AttachToGridOrMap();
            }

            OnRemovedFromBody(old);

            foreach (var mechanism in _mechanisms)
            {
                _entMan.EventBus.RaiseLocalEvent(mechanism.Owner, new RemovedFromBodyEvent(old), true);
            }
        }

        protected virtual void OnAddedToBody(SharedBodyComponent body) { }

        protected virtual void OnRemovedFromBody(SharedBodyComponent old) { }

        /// <summary>
        ///     Gibs the body part.
        /// </summary>
        public virtual HashSet<EntityUid> Gib()
        {
            var gibs = new HashSet<EntityUid>();

            foreach (var mechanism in _mechanisms)
            {
                gibs.Add(mechanism.Owner);
                RemoveMechanism(mechanism);
            }

            return gibs;
        }
    }

    [Serializable, NetSerializable]
    public sealed class BodyPartComponentState : ComponentState
    {
        [NonSerialized] private List<OrganComponent>? _mechanisms;

        public readonly EntityUid[] MechanismIds;

        public BodyPartComponentState(EntityUid[] mechanismIds)
        {
            MechanismIds = mechanismIds;
        }

        public List<OrganComponent> Mechanisms(IEntityManager? entityManager = null)
        {
            if (_mechanisms != null)
            {
                return _mechanisms;
            }

            IoCManager.Resolve(ref entityManager);

            var mechanisms = new List<OrganComponent>(MechanismIds.Length);

            foreach (var id in MechanismIds)
            {
                if (!entityManager.EntityExists(id))
                {
                    continue;
                }

                if (!entityManager.TryGetComponent(id, out OrganComponent? mechanism))
                {
                    continue;
                }

                mechanisms.Add(mechanism);
            }

            return _mechanisms = mechanisms;
        }
    }
}
