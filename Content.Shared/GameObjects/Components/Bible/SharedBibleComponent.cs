using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared.GameObjects.Components.Bible
{
    public class SharedBibleComponent : Component
    {
        public override string Name => "Bible";
        //public override uint? NetID => base.NetID;

        [Serializable, NetSerializable]
        public enum BibleUiKey
        {
            Key,
        }
    }

    [Serializable, NetSerializable]
    public enum BibleVisuals
    {
        Style,
    }

    [Serializable, NetSerializable]
    public class BibleBoundUserInterfaceState : BoundUserInterfaceState
    {
        public List<string> Styles;
        public BibleBoundUserInterfaceState(List<string> styles)
        {
            Styles = styles;
        }
    }

    [Serializable, NetSerializable]
    public class BibleSelectStyleMessage : BoundUserInterfaceMessage
    {
        public string Style;
        public BibleSelectStyleMessage(string style)
        {
            Style = style;
        }
    }
}
