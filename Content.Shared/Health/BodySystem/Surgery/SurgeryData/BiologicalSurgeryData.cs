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

namespace Content.Shared.BodySystem {

    /// <summary>
    ///     Data class representing the surgery state of a biological entity.
    /// </summary>	
    [NetSerializable, Serializable]
    public class BiologicalSurgeryData : ISurgeryData {

        private bool _skinOpened = false;
        private bool _skinPulled = false;

        public override SurgeryAction GetSurgeryStep(SurgeryToolType toolType)
        {
            if (!_skinOpened)
            {
                if (toolType == SurgeryToolType.Incision)
                    return OpenSkinSurgery;
            }
            else
            {
                if (toolType == SurgeryToolType.Cauterization)
                    return CautizerizeIncisionSurgery;
            }
            return null;
        }
        public override bool CanRemoveMechanisms()
        {
            if (_skinOpened)
                return true;
            else
                return false;
        }

        private void OpenSkinSurgery(IEntity performer)
        {
            ILocalizationManager localizationManager = IoCManager.Resolve<ILocalizationManager>();
            performer.PopupMessage(performer, localizationManager.GetString("Cut open the skin..."));
            //Delay?
            _skinOpened = true;
        }
        private void CautizerizeIncisionSurgery(IEntity performer)
        {
            ILocalizationManager localizationManager = IoCManager.Resolve<ILocalizationManager>();
            performer.PopupMessage(performer, localizationManager.GetString("Cauterize the incision..."));
            //Delay?
            _skinOpened = false;
        }
    }
}
