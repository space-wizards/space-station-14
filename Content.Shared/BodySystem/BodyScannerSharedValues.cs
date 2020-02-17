using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Content.Shared.BodySystem
{


    [NetSerializable, Serializable]
    public enum BodyScannerUiKey
    {
        Key
    }

    [NetSerializable, Serializable]
    public class BodyScannerInterfaceState : BoundUserInterfaceState
    {
        public readonly BodyTemplate Template;
        public readonly Dictionary<string, BodyPart> Parts;

        public BodyScannerInterfaceState(BodyTemplate _template, Dictionary<string, BodyPart> _parts)
        {
            Template = _template;
            Parts = _parts;
        }
    }
}


