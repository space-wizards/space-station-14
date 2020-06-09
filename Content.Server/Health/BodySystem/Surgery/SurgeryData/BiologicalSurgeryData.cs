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

namespace Content.Server.BodySystem {

    /// <summary>
    ///     Data class representing the surgery state of a biological entity.
    /// </summary>	
    public class BiologicalSurgeryData : ISurgeryData {

        protected bool _skinOpened = false;
        protected bool _vesselsClamped = false;
        protected bool _skinRetracted = false;
        protected Mechanism _targetOrgan; 

        public BiologicalSurgeryData(BodyPart parent) : base(parent) { }

        protected override bool CanInstallMechanism(Mechanism toBeInstalled)
        {
            return _skinOpened && _vesselsClamped && _skinRetracted;
        }

        public override SurgeryAction GetSurgeryStep(SurgeryToolType toolType)
        {
            if (toolType == SurgeryToolType.BoneSawing)
            {
                return RemoveBodyPartSurgery;
            }
            if (_skinOpened)
            {
                if (_vesselsClamped)
                {
                    if (_skinRetracted) 
                    {
                        if (_targetOrgan != null && toolType == SurgeryToolType.VesselCompression)
                            return RemoveOrganSurgery;
                        if (toolType == SurgeryToolType.Incision) //_targetOrgan is potentially given a value by DisconnectOrganSurgery.
                            return DisconnectOrganSurgery;
                        else if (toolType == SurgeryToolType.Cauterization)
                            return CautizerizeIncisionSurgery;
                    }
                    else
                    {
                        if (toolType == SurgeryToolType.Retraction)
                            return RetractSkinSurgery;
                        else if (toolType == SurgeryToolType.Cauterization)
                            return CautizerizeIncisionSurgery;
                    }
                }
                else
                {
                    if (toolType == SurgeryToolType.VesselCompression)
                        return ClampVesselsSurgery;
                    else if (toolType == SurgeryToolType.Cauterization)
                        return CautizerizeIncisionSurgery;
                }
            }
            else
            {
                if (toolType == SurgeryToolType.Incision)
                    return OpenSkinSurgery;
            }
            return null;
        }

        protected void OpenSkinSurgery(IBodyPartContainer container, IEntity performer)
        {
            ILocalizationManager localizationManager = IoCManager.Resolve<ILocalizationManager>();
            performer.PopupMessage(performer, localizationManager.GetString("Cut open the skin..."));
            //Delay?
            _skinOpened = true;
        }
        protected void ClampVesselsSurgery(IBodyPartContainer container, IEntity performer)
        {
            ILocalizationManager localizationManager = IoCManager.Resolve<ILocalizationManager>();
            performer.PopupMessage(performer, localizationManager.GetString("Clamp the vessels..."));
            //Delay?
            _vesselsClamped = true;
        }
        protected void RetractSkinSurgery(IBodyPartContainer container, IEntity performer)
        {
            ILocalizationManager localizationManager = IoCManager.Resolve<ILocalizationManager>();
            performer.PopupMessage(performer, localizationManager.GetString("Retract the skin..."));
            //Delay?
            _skinRetracted = true;
        }
        protected void CautizerizeIncisionSurgery(IBodyPartContainer container, IEntity performer)
        {
            ILocalizationManager localizationManager = IoCManager.Resolve<ILocalizationManager>();
            performer.PopupMessage(performer, localizationManager.GetString("Cauterize the incision..."));
            //Delay?
            _skinOpened = false;
            _vesselsClamped = false;
            _skinRetracted = false;
        }
        protected void DisconnectOrganSurgery(IBodyPartContainer container, IEntity performer)
        {
            Mechanism mechanismTarget = null;
            //TODO: figureout popup, right now it just takes the first organ available if there is one
            if (_parent.Mechanisms.Count > 0)
                mechanismTarget = _parent.Mechanisms[0];
            if (mechanismTarget != null)
            {
                ILocalizationManager localizationManager = IoCManager.Resolve<ILocalizationManager>();
                performer.PopupMessage(performer, localizationManager.GetString("Detach the organ..."));
                //Delay?
                _targetOrgan = mechanismTarget;
            }

        }
        protected void RemoveOrganSurgery(IBodyPartContainer container, IEntity performer)
        {
            if (_targetOrgan != null)
            {
                ILocalizationManager localizationManager = IoCManager.Resolve<ILocalizationManager>();
                performer.PopupMessage(performer, localizationManager.GetString("Remove the organ..."));
                //Delay?
                _parent.DropMechanism(performer, _targetOrgan);
            }
        }

        protected void RemoveBodyPartSurgery(IBodyPartContainer container, IEntity performer)
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
