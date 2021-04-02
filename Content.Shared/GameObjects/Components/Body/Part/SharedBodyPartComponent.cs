#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Surgery;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Body.Part
{
    public abstract class SharedBodyPartComponent : Component, IBodyPart
    {
        public override string Name => "BodyPart";

        public override uint? NetID => ContentNetIDs.BODY_PART;

        private IBody? _body;

        // TODO BODY Remove
        [DataField("mechanisms")]
        private List<string> _mechanismIds = new();
        public IReadOnlyList<string> MechanismIds => _mechanismIds;

        [ViewVariables]
        private readonly HashSet<IMechanism> _mechanisms = new();

        [ViewVariables]
        public IBody? Body
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

        [ViewVariables]
        [DataField("partType")]
        public BodyPartType PartType { get; private set; } = BodyPartType.Other;

        [ViewVariables] [DataField("size")] public int Size { get; private set; } = 1;

        [ViewVariables] public int SizeUsed { get; private set; }

        // TODO BODY size used
        // TODO BODY surgerydata

        /// <summary>
        ///     What types of BodyParts this <see cref="IBodyPart"/> can easily attach to.
        ///     For the most part, most limbs aren't universal and require extra work to
        ///     attach between types.
        /// </summary>
        [ViewVariables]
        [DataField("compatibility")]
        public BodyPartCompatibility Compatibility { get; private set; } = BodyPartCompatibility.Universal;

        /// <summary>
        ///     Set of all <see cref="IMechanism"/> currently inside this
        ///     <see cref="IBodyPart"/>.
        /// </summary>
        [ViewVariables]
        public IReadOnlyCollection<IMechanism> Mechanisms => _mechanisms;

        // TODO BODY Replace with a simulation of organs
        /// <summary>
        ///     Represents if body part is vital for creature.
        ///     If the last vital body part is removed creature dies
        /// </summary>
        [ViewVariables]
        [DataField("vital")]
        public bool IsVital { get; private set; } = false;

        [ViewVariables]
        [DataField("symmetry")]
        public BodyPartSymmetry Symmetry { get; private set; } = BodyPartSymmetry.None;

        [ViewVariables]
        public ISurgeryData? SurgeryDataComponent => Owner.GetComponentOrNull<ISurgeryData>();

        protected virtual void OnAddMechanism(IMechanism mechanism)
        {
            var prototypeId = mechanism.Owner.Prototype!.ID;

            if (!_mechanismIds.Contains(prototypeId))
            {
                _mechanismIds.Add(prototypeId);
            }

            mechanism.Part = this;
            SizeUsed += mechanism.Size;

            Dirty();
        }

        protected virtual void OnRemoveMechanism(IMechanism mechanism)
        {
            _mechanismIds.Remove(mechanism.Owner.Prototype!.ID);
            mechanism.Part = null;
            SizeUsed -= mechanism.Size;

            Dirty();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            var mechanismIds = new EntityUid[_mechanisms.Count];

            var i = 0;
            foreach (var mechanism in _mechanisms)
            {
                mechanismIds[i] = mechanism.Owner.Uid;
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

        public bool SurgeryCheck(SurgeryType surgery)
        {
            return SurgeryDataComponent?.CheckSurgery(surgery) ?? false;
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

            return SurgeryDataComponent?.PerformSurgery(toolType, target, surgeon, performer) ?? false;
        }

        public bool CanAttachPart(IBodyPart part)
        {
            DebugTools.AssertNotNull(part);

            return SurgeryDataComponent?.CanAttachBodyPart(part) ?? false;
        }

        public virtual bool CanAddMechanism(IMechanism mechanism)
        {
            DebugTools.AssertNotNull(mechanism);

            return SurgeryDataComponent != null &&
                   SizeUsed + mechanism.Size <= Size &&
                   SurgeryDataComponent.CanAddMechanism(mechanism);
        }

        /// <summary>
        ///     Tries to add a mechanism onto this body part.
        /// </summary>
        /// <param name="mechanism">The mechanism to try to add.</param>
        /// <param name="force">
        ///     Whether or not to check if the mechanism can be added.
        /// </param>
        /// <returns>
        ///     True if successful, false if there was an error
        ///     (e.g. not enough room in <see cref="IBodyPart"/>).
        ///     Will return false even when forced if the mechanism is already
        ///     added in this <see cref="IBodyPart"/>.
        /// </returns>
        public bool TryAddMechanism(IMechanism mechanism, bool force = false)
        {
            DebugTools.AssertNotNull(mechanism);

            if (!force && !CanAddMechanism(mechanism))
            {
                return false;
            }

            if (!_mechanisms.Add(mechanism))
            {
                return false;
            }

            OnAddMechanism(mechanism);

            return true;
        }

        public bool RemoveMechanism(IMechanism mechanism)
        {
            DebugTools.AssertNotNull(mechanism);

            if (!_mechanisms.Remove(mechanism))
            {
                return false;
            }

            OnRemoveMechanism(mechanism);

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

        private void AddedToBody(IBody body)
        {
            Owner.Transform.LocalRotation = 0;
            Owner.Transform.AttachParent(body.Owner);
            OnAddedToBody(body);

            foreach (var mechanism in _mechanisms)
            {
                mechanism.AddedToBody(body);
            }
        }

        private void RemovedFromBody(IBody old)
        {
            if (!Owner.Transform.Deleted)
            {
                Owner.Transform.AttachToGridOrMap();
            }

            OnRemovedFromBody(old);

            foreach (var mechanism in _mechanisms)
            {
                mechanism.RemovedFromBody(old);
            }
        }

        protected virtual void OnAddedToBody(IBody body) { }

        protected virtual void OnRemovedFromBody(IBody old) { }

        public virtual void Gib()
        {
            foreach (var mechanism in _mechanisms)
            {
                RemoveMechanism(mechanism);
            }
        }
    }

    [Serializable, NetSerializable]
    public class BodyPartComponentState : ComponentState
    {
        [NonSerialized] private List<IMechanism>? _mechanisms;

        public readonly EntityUid[] MechanismIds;

        public BodyPartComponentState(EntityUid[] mechanismIds) : base(ContentNetIDs.BODY_PART)
        {
            MechanismIds = mechanismIds;
        }

        public List<IMechanism> Mechanisms(IEntityManager? entityManager = null)
        {
            if (_mechanisms != null)
            {
                return _mechanisms;
            }

            entityManager ??= IoCManager.Resolve<IEntityManager>();

            var mechanisms = new List<IMechanism>(MechanismIds.Length);

            foreach (var id in MechanismIds)
            {
                if (!entityManager.TryGetEntity(id, out var entity))
                {
                    continue;
                }

                if (!entity.TryGetComponent(out IMechanism? mechanism))
                {
                    continue;
                }

                mechanisms.Add(mechanism);
            }

            return _mechanisms = mechanisms;
        }
    }
}
