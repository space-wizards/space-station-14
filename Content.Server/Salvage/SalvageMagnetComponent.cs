using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Examine;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using Robust.Shared.Maths;

namespace Content.Server.Salvage
{
    /// <summary>
    ///     A salvage magnet.
    /// </summary>
    [RegisterComponent]
    [ComponentProtoName("SalvageMagnet")]
    public class SalvageMagnetComponent : Component
    {
        /// <summary>
        ///     Offset relative to magnet that salvage should spawn.
        ///     Keep in sync with marker sprite (if any???)
        /// </summary>
        [ViewVariables]
        [DataField("offset")]
        public Vector2 Offset = Vector2.Zero;
    }
}
