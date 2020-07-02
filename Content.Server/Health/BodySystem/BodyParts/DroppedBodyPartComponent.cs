using System.Globalization;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.Health.BodySystem.BodyParts {

    /// <summary>
    ///    Component containing the data for a dropped BodyPart entity.
    /// </summary>
    [RegisterComponent]
    public class DroppedBodyPartComponent : Component {

        public sealed override string Name => "DroppedBodyPart";

        [ViewVariables]
        private BodyPart _containedBodyPart;

        public void TransferBodyPartData(BodyPart data)
        {
            _containedBodyPart = data;
            Owner.Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(_containedBodyPart.Name);
            if (Owner.TryGetComponent(out SpriteComponent component))
            {
                component.LayerSetRSI(0, data.RSIPath);
                component.LayerSetState(0, data.RSIState);
            }
        }
    }
}
