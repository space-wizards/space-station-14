using Content.Server.BodySystem;
using Content.Server.Health.BodySystem.BodyParts;
using Content.Shared.BodySystem;
using Content.Shared.Interfaces;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Health.BodySystem.Surgery.SurgeryData
{
    /// <summary>
    ///     Data class representing the surgery state of a biological entity.
    /// </summary>
    public class BiologicalSurgeryData : SurgeryData
    {
        protected bool SkinOpened;
        protected bool VesselsClamped;
        protected bool SkinRetracted;
        protected Mechanism TargetOrgan;

        public BiologicalSurgeryData(BodyPart parent) : base(parent) { }

        public override SurgeryAction GetSurgeryStep(SurgeryToolType toolType)
        {
            if (SkinOpened)
            {
                if (VesselsClamped)
                {
                    if (SkinRetracted)
                    {
                        if (TargetOrgan != null && toolType == SurgeryToolType.VesselCompression)
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

        protected void OpenSkinSurgery(BodyManagerComponent target, IEntity performer)
        {
            ILocalizationManager localizationManager = IoCManager.Resolve<ILocalizationManager>();
            performer.PopupMessage(performer, localizationManager.GetString("Cut open the skin..."));
            //Delay?
            SkinOpened = true;
        }
        protected void ClampVesselsSurgery(BodyManagerComponent target, IEntity performer)
        {
            ILocalizationManager localizationManager = IoCManager.Resolve<ILocalizationManager>();
            performer.PopupMessage(performer, localizationManager.GetString("Clamp the vessels..."));
            //Delay?
            VesselsClamped = true;
        }
        protected void RetractSkinSurgery(BodyManagerComponent target, IEntity performer)
        {
            ILocalizationManager localizationManager = IoCManager.Resolve<ILocalizationManager>();
            performer.PopupMessage(performer, localizationManager.GetString("Retract the skin..."));
            //Delay?
            SkinRetracted = true;
        }
        protected void CautizerizeIncisionSurgery(BodyManagerComponent target, IEntity performer)
        {
            ILocalizationManager localizationManager = IoCManager.Resolve<ILocalizationManager>();
            performer.PopupMessage(performer, localizationManager.GetString("Cauterize the incision..."));
            //Delay?
            SkinOpened = false;
            VesselsClamped = false;
            SkinRetracted = false;
        }
        protected void DisconnectOrganSurgery(BodyManagerComponent target, IEntity performer)
        {
            Mechanism mechanismTarget = null;
            //TODO: figureout popup, right now it just takes the first organ available if there is one
            if (Parent.Mechanisms.Count > 0)
                mechanismTarget = Parent.Mechanisms[0];
            if (mechanismTarget != null)
            {
                ILocalizationManager localizationManager = IoCManager.Resolve<ILocalizationManager>();
                performer.PopupMessage(performer, localizationManager.GetString("Detach the organ..."));
                //Delay?
                TargetOrgan = mechanismTarget;
            }

        }
        protected void RemoveOrganSurgery(BodyManagerComponent target, IEntity performer)
        {
            if (TargetOrgan != null)
            {
                ILocalizationManager localizationManager = IoCManager.Resolve<ILocalizationManager>();
                performer.PopupMessage(performer, localizationManager.GetString("Remove the organ..."));
                //Delay?
                Parent.DropMechanism(performer, TargetOrgan);
            }
        }
    }
}
