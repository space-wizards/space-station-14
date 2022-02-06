using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Tabletop.Components
{
    /// <summary>
    ///     Component for marking an entity as currently playing a tabletop.
    /// </summary>
    [RegisterComponent, Friend(typeof(TabletopSystem))]
    public class TabletopGamerComponent : Component
    {
        [DataField("tabletop")]
        public EntityUid Tabletop { get; set; } = EntityUid.Invalid;
    }
}
