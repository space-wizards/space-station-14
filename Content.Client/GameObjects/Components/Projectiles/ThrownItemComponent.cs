using Content.Shared.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Projectiles
{
    [RegisterComponent]
    public class ThrownItemComponent : ProjectileComponent
    {
        public override string Name => "ThrownItem";
        public override uint? NetID => ContentNetIDs.THROWN_ITEM;
    }
}
