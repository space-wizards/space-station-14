using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.PackageWrapper.Components
{
    [RegisterComponent]
    public class WrapableShapeComponent : Component
    {
        public sealed override string Name => "WrapType";

        [DataField("wrapIn")]
        public string WrapType { get; } = string.Empty;
    }
}
