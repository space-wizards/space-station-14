using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared.GameObjects.Components
{
    public class SharedCrayonComponent
    {
        [NetSerializable]
        [Serializable]
        public enum CrayonVisuals
        {
            State,
            Color
        }
    }
}
