using System;
using System.Collections.Generic;
using Content.Shared.BodySystem;
using Content.Shared.Interfaces;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Server.BodySystem
{

    /// <summary>
    ///     Data class representing the surgery state of a biological entity.
    /// </summary>	
    public class BiologicalSurgeryData : ISurgeryData
    {

        protected bool _skinOpened = false;
        protected bool _vesselsClamped = false;
        protected bool _skinRetracted = false;
        protected List<Mechanism> _disconnectedOrgans = new List<Mechanism>();

        public BiologicalSurgeryData(BodyPart parent) : base(parent) { }

        public override bool CanInstallMechanism(Mechanism toBeInstalled)
        {
            return _skinOpened && _vesselsClamped && _skinRetracted;
        }



        public override SurgeryAction GetSurgeryStep(SurgeryType toolType)
        {
            if (toolType == SurgeryType.Amputation)
            {
                return RemoveBodyPartSurgery;
            }
            if (!_skinOpened) //Case: skin is normal.
            {
                if (toolType == SurgeryType.Incision)
                    return OpenSkinSurgery;
            }
            else if (_skinOpened && !_vesselsClamped) //Case: skin is opened, but not clamped.
            {
                if (toolType == SurgeryType.VesselCompression)
                    return ClampVesselsSurgery;
                else if (toolType == SurgeryType.Cauterization)
                    return CautizerizeIncisionSurgery;
            }
            else if (_skinOpened && _vesselsClamped && !_skinRetracted) //Case: skin is opened and clamped, but not retracted.
            {
                if (toolType == SurgeryType.Retraction)
                    return RetractSkinSurgery;
                else if (toolType == SurgeryType.Cauterization)
                    return CautizerizeIncisionSurgery;
            }
            else if (_skinOpened && _vesselsClamped && _skinRetracted) //Case: skin is fully open.
            {
                if (_parent.Mechanisms.Count > 0 && toolType == SurgeryType.VesselCompression)
                    return LoosenOrganSurgery;
                else if (_disconnectedOrgans.Count > 0 && toolType == SurgeryType.Incision)
                    return RemoveOrganSurgery;
                else if (toolType == SurgeryType.Cauterization)
                    return CautizerizeIncisionSurgery;
            }
            return null;
        }



        public override string GetDescription()
        {
            string toReturn = "";
            if (_skinOpened && !_vesselsClamped) //Case: skin is opened, but not clamped.
            {
                toReturn += "The skin on his " + _parent.Name + " has an incision, but it is prone to bleeding.\n";
            }
            else if (_skinOpened && _vesselsClamped && !_skinRetracted) //Case: skin is opened and clamped, but not retracted.
            {
                toReturn += "The skin on his " + _parent.Name + " has an incision, but it is not retracted.\n";
            }
            else if (_skinOpened && _vesselsClamped && _skinRetracted) //Case: skin is fully open.
            {
                toReturn += "There is an incision on his " + _parent.Name + ".\n";
                foreach (Mechanism mechanism in _disconnectedOrgans)
                {
                    toReturn += "His " + mechanism.Name + " is loose.\n";
                }
            }
            return toReturn;
        }



        protected void OpenSkinSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            ILocalizationManager localizationManager = IoCManager.Resolve<ILocalizationManager>();
            performer.PopupMessage(performer, localizationManager.GetString("Cut open the skin..."));
            //Delay?
            _skinOpened = true;
        }
        protected void ClampVesselsSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            ILocalizationManager localizationManager = IoCManager.Resolve<ILocalizationManager>();
            performer.PopupMessage(performer, localizationManager.GetString("Clamp the vessels..."));
            //Delay?
            _vesselsClamped = true;
        }
        protected void RetractSkinSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            ILocalizationManager localizationManager = IoCManager.Resolve<ILocalizationManager>();
            performer.PopupMessage(performer, localizationManager.GetString("Retract the skin..."));
            //Delay?
            _skinRetracted = true;
        }
        protected void CautizerizeIncisionSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            ILocalizationManager localizationManager = IoCManager.Resolve<ILocalizationManager>();
            performer.PopupMessage(performer, localizationManager.GetString("Cauterize the incision..."));
            //Delay?
            _skinOpened = false;
            _vesselsClamped = false;
            _skinRetracted = false;
        }
        protected void LoosenOrganSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (_parent.Mechanisms.Count <= 0)
                return;
            surgeon.RequestMechanism(_parent.Mechanisms, LoosenOrganSurgeryCallback);
        }
        public void LoosenOrganSurgeryCallback(Mechanism target, IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (target != null && _parent.Mechanisms.Contains(target))
            {
                ILocalizationManager localizationManager = IoCManager.Resolve<ILocalizationManager>();
                performer.PopupMessage(performer, localizationManager.GetString("Loosen the organ..."));
                //Delay?
                _disconnectedOrgans.Add(target);
            }
        }
        protected void RemoveOrganSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (_disconnectedOrgans.Count <= 0)
                return;
            if (_disconnectedOrgans.Count == 1)
                RemoveOrganSurgeryCallback(_disconnectedOrgans[0], container, surgeon, performer);
            else
                surgeon.RequestMechanism(_parent.Mechanisms, RemoveOrganSurgeryCallback);


        }
        public void RemoveOrganSurgeryCallback(Mechanism target, IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (target != null && _parent.Mechanisms.Contains(target))
            {
                ILocalizationManager localizationManager = IoCManager.Resolve<ILocalizationManager>();
                performer.PopupMessage(performer, localizationManager.GetString("Remove the organ..."));
                //Delay?
                _parent.DropMechanism(performer, target);
                _disconnectedOrgans.Remove(target);
            }
        }
        protected void RemoveBodyPartSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (!(container is BodyManagerComponent)) //This surgery requires a DroppedBodyPartComponent.
                return;
            BodyManagerComponent bmTarget = (BodyManagerComponent) container;
            ILocalizationManager localizationManager = IoCManager.Resolve<ILocalizationManager>();
            performer.PopupMessage(performer, localizationManager.GetString("Saw off the limb!"));
            //Delay?
            bmTarget.DisconnectBodyPart(_parent, true);
        }


    }
}
