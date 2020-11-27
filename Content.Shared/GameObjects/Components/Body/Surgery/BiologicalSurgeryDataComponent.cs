#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;

namespace Content.Shared.GameObjects.Components.Body.Surgery
{
    /// <summary>
    ///     Data class representing the surgery state of a biological entity.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(SurgeryDataComponent))]
    public class BiologicalSurgeryDataComponent : SurgeryDataComponent
    {
        public override string Name => "BiologicalSurgeryData";

        private readonly List<IMechanism> _disconnectedOrgans = new List<IMechanism>();

        private bool _skinOpened;
        private bool _skinRetracted;
        private bool _vesselsClamped;

        protected override SurgeryAction? GetSurgeryStep(SurgeryType toolType)
        {
            if (Parent == null)
            {
                return null;
            }

            if (toolType == SurgeryType.Amputation)
            {
                return RemoveBodyPartSurgery;
            }

            if (!_skinOpened)
            {
                // Case: skin is normal.
                if (toolType == SurgeryType.Incision)
                {
                    return OpenSkinSurgery;
                }
            }
            else if (!_vesselsClamped)
            {
                // Case: skin is opened, but not clamped.
                switch (toolType)
                {
                    case SurgeryType.VesselCompression:
                        return ClampVesselsSurgery;
                    case SurgeryType.Cauterization:
                        return CauterizeIncisionSurgery;
                }
            }
            else if (!_skinRetracted)
            {
                // Case: skin is opened and clamped, but not retracted.
                switch (toolType)
                {
                    case SurgeryType.Retraction:
                        return RetractSkinSurgery;
                    case SurgeryType.Cauterization:
                        return CauterizeIncisionSurgery;
                }
            }
            else
            {
                // Case: skin is fully open.
                if (Parent.Mechanisms.Count > 0 &&
                    toolType == SurgeryType.VesselCompression)
                {
                    if (_disconnectedOrgans.Except(Parent.Mechanisms).Count() != 0 ||
                        Parent.Mechanisms.Except(_disconnectedOrgans).Count() != 0)
                    {
                        return LoosenOrganSurgery;
                    }
                }

                if (_disconnectedOrgans.Count > 0 && toolType == SurgeryType.Incision)
                {
                    return RemoveOrganSurgery;
                }

                if (toolType == SurgeryType.Cauterization)
                {
                    return CauterizeIncisionSurgery;
                }
            }

            return null;
        }

        public override string GetDescription()
        {
            if (Parent == null)
            {
                return "";
            }

            var toReturn = "";

            if (_skinOpened && !_vesselsClamped)
            {
                // Case: skin is opened, but not clamped.
                toReturn += Loc.GetString("The skin on {0:their} {1} has an incision, but it is prone to bleeding.\n",
                    Owner, Parent.Name);
            }
            else if (_skinOpened && _vesselsClamped && !_skinRetracted)
            {
                // Case: skin is opened and clamped, but not retracted.
                toReturn += Loc.GetString("The skin on {0:their} {1} has an incision, but it is not retracted.\n",
                    Owner, Parent.Name);
            }
            else if (_skinOpened && _vesselsClamped && _skinRetracted)
            {
                // Case: skin is fully open.
                toReturn += Loc.GetString("There is an incision on {0:their} {1}.\n", Owner, Parent.Name);
                foreach (var mechanism in _disconnectedOrgans)
                {
                    toReturn += Loc.GetString("{0:their} {1} is loose.\n", Owner, mechanism.Name);
                }
            }

            return toReturn;
        }

        public override bool CanAddMechanism(IMechanism mechanism)
        {
            return Parent != null &&
                   _skinOpened &&
                   _vesselsClamped &&
                   _skinRetracted;
        }

        public override bool CanAttachBodyPart(IBodyPart part)
        {
            return Parent != null;
            // TODO BODY if a part is disconnected, you should have to do some surgery to allow another bodypart to be attached.
        }

        private void OpenSkinSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (Parent == null) return;

                performer.PopupMessage(Loc.GetString("Cut open the skin..."));

            // TODO BODY do_after: Delay
            _skinOpened = true;
        }

        private void ClampVesselsSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (Parent == null) return;

            performer.PopupMessage(Loc.GetString("Clamp the vessels..."));

            // TODO BODY do_after: Delay
            _vesselsClamped = true;
        }

        private void RetractSkinSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (Parent == null) return;

            performer.PopupMessage(Loc.GetString("Retract the skin..."));

            // TODO BODY do_after: Delay
            _skinRetracted = true;
        }

        private void CauterizeIncisionSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (Parent == null) return;

            performer.PopupMessage(Loc.GetString("Cauterize the incision..."));

            // TODO BODY do_after: Delay
            _skinOpened = false;
            _vesselsClamped = false;
            _skinRetracted = false;
        }

        private void LoosenOrganSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (Parent == null) return;
            if (Parent.Mechanisms.Count <= 0) return;

            var toSend = new List<IMechanism>();
            foreach (var mechanism in Parent.Mechanisms)
            {
                if (!_disconnectedOrgans.Contains(mechanism))
                {
                    toSend.Add(mechanism);
                }
            }

            if (toSend.Count > 0)
            {
                surgeon.RequestMechanism(toSend, LoosenOrganSurgeryCallback);
            }
        }

        private void LoosenOrganSurgeryCallback(IMechanism? target, IBodyPartContainer container, ISurgeon surgeon,
            IEntity performer)
        {
            if (Parent == null || target == null || !Parent.Mechanisms.Contains(target))
            {
                return;
            }

            performer.PopupMessage(Loc.GetString("Loosen the organ..."));

            // TODO BODY do_after: Delay
            _disconnectedOrgans.Add(target);
        }

        private void RemoveOrganSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (Parent == null) return;

            if (_disconnectedOrgans.Count <= 0)
            {
                return;
            }

            if (_disconnectedOrgans.Count == 1)
            {
                RemoveOrganSurgeryCallback(_disconnectedOrgans[0], container, surgeon, performer);
            }
            else
            {
                surgeon.RequestMechanism(_disconnectedOrgans, RemoveOrganSurgeryCallback);
            }
        }

        private void RemoveOrganSurgeryCallback(IMechanism? target, IBodyPartContainer container, ISurgeon surgeon,
            IEntity performer)
        {
            if (Parent == null || target == null || !Parent.Mechanisms.Contains(target))
            {
                return;
            }

            performer.PopupMessage(Loc.GetString("Remove the organ..."));

            // TODO BODY do_after: Delay
            Parent.RemoveMechanism(target, performer.Transform.Coordinates);
            _disconnectedOrgans.Remove(target);
        }

        private void RemoveBodyPartSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (Parent == null) return;
            if (container is not IBody body) return;

            performer.PopupMessage(Loc.GetString("Saw off the limb!"));

            // TODO BODY do_after: Delay
            body.RemovePart(Parent);
        }
    }
}
