using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using Content.Shared.BodySystem;
using Robust.Shared.ViewVariables;
using System.Globalization;
using Robust.Server.GameObjects;
using Content.Server.GameObjects.EntitySystems;

namespace Content.Server.BodySystem
{

    /// <summary>
    ///    Component containing the data for a dropped BodyPart entity.
    /// </summary>	
    [RegisterComponent]
    public class DroppedBodyPartComponent : Component, IAfterAttack, IBodyPartContainer
    {

        public sealed override string Name => "DroppedBodyPart";

        [ViewVariables]
        public BodyPart ContainedBodyPart { get; set; }

        public void TransferBodyPartData(BodyPart data)
        {
            ContainedBodyPart = data;
            Owner.Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ContainedBodyPart.Name);
            if (Owner.TryGetComponent<SpriteComponent>(out SpriteComponent component))
            {
                component.LayerSetRSI(0, data.RSIPath);
                component.LayerSetState(0, data.RSIState);
            }
        }

        void IAfterAttack.AfterAttack(AfterAttackEventArgs eventArgs)
        {
            if (eventArgs.Attacked == null)
                return;
            if (eventArgs.Attacked.TryGetComponent<BodyManagerComponent>(out BodyManagerComponent bodyManager))
            {
                //Popup UI to possibly install limb on someone.
            }
        }
    }
}
