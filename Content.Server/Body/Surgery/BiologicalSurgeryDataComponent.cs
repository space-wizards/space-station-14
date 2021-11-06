using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.DoAfter;
using Content.Shared.Body.Components;
using Content.Shared.Body.Mechanism;
using Content.Shared.Body.Part;
using Content.Shared.Body.Surgery;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using static Content.Shared.Body.Surgery.ISurgeryData;

namespace Content.Server.Body.Surgery
{
    /// <summary>
    ///     Data class representing the surgery state of a biological entity.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(ISurgeryData))]
    public class BiologicalSurgeryDataComponent : Component, ISurgeryData
    {
        public override string Name => "BiologicalSurgeryData";

        private readonly HashSet<SharedMechanismComponent> _disconnectedOrgans = new();

        private bool SkinOpened { get; set; }

        private bool SkinRetracted { get; set; }

        private bool VesselsClamped { get; set; }

        public SharedBodyPartComponent? Parent => Owner.GetComponentOrNull<SharedBodyPartComponent>();

        public BodyPartType? ParentType => Parent?.PartType;

        private void AddDisconnectedOrgan(SharedMechanismComponent mechanism)
        {
            if (_disconnectedOrgans.Add(mechanism))
            {
                Dirty();
            }
        }

        private void RemoveDisconnectedOrgan(SharedMechanismComponent mechanism)
        {
            if (_disconnectedOrgans.Remove(mechanism))
            {
                Dirty();
            }
        }

        private async Task<bool> SurgeryDoAfter(IEntity performer)
        {
            if (!performer.HasComponent<DoAfterComponent>())
            {
                return true;
            }

            var doAfterSystem = EntitySystem.Get<DoAfterSystem>();
            var target = Parent?.Body?.Owner ?? Owner;
            var args = new DoAfterEventArgs(performer, 3, target: target)
            {
                BreakOnUserMove = true,
                BreakOnTargetMove = true
            };

            return await doAfterSystem.WaitDoAfter(args) == DoAfterStatus.Finished;
        }

        private bool HasIncisionNotClamped()
        {
            return SkinOpened && !VesselsClamped;
        }

        private bool HasClampedIncisionNotRetracted()
        {
            return SkinOpened && VesselsClamped && !SkinRetracted;
        }

        private bool HasFullyOpenIncision()
        {
            return SkinOpened && VesselsClamped && SkinRetracted;
        }

        public string GetDescription()
        {
            if (Parent == null)
            {
                return string.Empty;
            }

            var toReturn = new StringBuilder();

            if (HasIncisionNotClamped())
            {
                toReturn.Append(Loc.GetString("biological-surgery-data-component-has-incision-not-clamped-message",
                    ("owner", Owner),("bodyPart", Parent.Name)));
            }
            else if (HasClampedIncisionNotRetracted())
            {
                toReturn.AppendLine(Loc.GetString("biological-surgery-data-component-has-clamped-incision-not-retracted-message",
                    ("owner", Owner),("bodyPary", Parent.Name)));
            }
            else if (HasFullyOpenIncision())
            {
                toReturn.AppendLine(Loc.GetString("biological-surgery-data-component-has-fully-open-incision-message", ("owner", Owner), ("bodyPart", Parent.Name)) + "\n");
                foreach (var mechanism in _disconnectedOrgans)
                {
                    toReturn.AppendLine(Loc.GetString("biological-surgery-data-component-part-is-loose-message",("owner", Owner), ("bodyPart", mechanism.Name)));
                }
            }

            return toReturn.ToString();
        }

        public bool CanAddMechanism(SharedMechanismComponent mechanism)
        {
            return Parent != null &&
                   SkinOpened &&
                   VesselsClamped &&
                   SkinRetracted;
        }

        public bool CanAttachBodyPart(SharedBodyPartComponent part)
        {
            return Parent != null;
            // TODO BODY if a part is disconnected, you should have to do some surgery to allow another body part to be attached.
        }

        public SurgeryAction? GetSurgeryStep(SurgeryType toolType)
        {
            if (Parent == null)
            {
                return null;
            }

            if (toolType == SurgeryType.Amputation)
            {
                return RemoveBodyPartSurgery;
            }

            if (!SkinOpened)
            {
                // Case: skin is normal.
                if (toolType == SurgeryType.Incision)
                {
                    return OpenSkinSurgery;
                }
            }
            else if (!VesselsClamped)
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
            else if (!SkinRetracted)
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

        public bool CheckSurgery(SurgeryType toolType)
        {
            return GetSurgeryStep(toolType) != null;
        }

        public bool PerformSurgery(SurgeryType surgeryType, IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            var step = GetSurgeryStep(surgeryType);

            if (step == null)
            {
                return false;
            }

            step(container, surgeon, performer);
            return true;
        }

        private async void OpenSkinSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (Parent == null)
            {
                return;
            }

            performer.PopupMessage(Loc.GetString("biological-surgery-data-component-open-skin-message"));

            if (await SurgeryDoAfter(performer))
            {
                SkinOpened = true;
            }
        }

        private async void ClampVesselsSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (Parent == null) return;

            performer.PopupMessage(Loc.GetString("biological-surgery-data-component-clamp-vessels-message"));

            if (await SurgeryDoAfter(performer))
            {
                VesselsClamped = true;
            }
        }

        private async void RetractSkinSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (Parent == null) return;

            performer.PopupMessage(Loc.GetString("biological-surgery-data-component-retract-skin-message"));

            if (await SurgeryDoAfter(performer))
            {
                SkinRetracted = true;
            }
        }

        private async void CauterizeIncisionSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (Parent == null) return;

            performer.PopupMessage(Loc.GetString("biological-surgery-data-component-cauterize-incision-message"));

            if (await SurgeryDoAfter(performer))
            {
                SkinOpened = false;
                VesselsClamped = false;
                SkinRetracted = false;
            }
        }

        private void LoosenOrganSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (Parent == null) return;
            if (Parent.Mechanisms.Count <= 0) return;

            var toSend = new List<SharedMechanismComponent>();
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

        private async void LoosenOrganSurgeryCallback(SharedMechanismComponent? target, IBodyPartContainer container, ISurgeon surgeon,
            IEntity performer)
        {
            if (Parent == null || target == null || !Parent.Mechanisms.Contains(target))
            {
                return;
            }

            performer.PopupMessage(Loc.GetString("biological-surgery-data-component-loosen-organ-message"));

            if (!performer.HasComponent<DoAfterComponent>())
            {
                AddDisconnectedOrgan(target);
                return;
            }

            if (await SurgeryDoAfter(performer))
            {
                AddDisconnectedOrgan(target);
            }
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
                RemoveOrganSurgeryCallback(_disconnectedOrgans.First(), container, surgeon, performer);
            }
            else
            {
                surgeon.RequestMechanism(_disconnectedOrgans, RemoveOrganSurgeryCallback);
            }
        }

        private async void RemoveOrganSurgeryCallback(SharedMechanismComponent? target, IBodyPartContainer container, ISurgeon surgeon,
            IEntity performer)
        {
            if (Parent == null || target == null || !Parent.Mechanisms.Contains(target))
            {
                return;
            }

            performer.PopupMessage(Loc.GetString("biological-surgery-data-component-remove-organ-message"));

            if (!performer.HasComponent<DoAfterComponent>())
            {
                Parent.RemoveMechanism(target, performer.Transform.Coordinates);
                RemoveDisconnectedOrgan(target);
                return;
            }

            if (await SurgeryDoAfter(performer))
            {
                Parent.RemoveMechanism(target, performer.Transform.Coordinates);
                RemoveDisconnectedOrgan(target);
            }
        }

        private async void RemoveBodyPartSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (Parent == null) return;
            if (container is not SharedBodyComponent body) return;

            performer.PopupMessage(Loc.GetString("biological-surgery-data-component-remove-bodypart-message"));

            if (await SurgeryDoAfter(performer))
            {
                body.RemovePart(Parent);
            }
        }
    }
}
