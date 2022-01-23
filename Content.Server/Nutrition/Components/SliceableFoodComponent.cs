using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Sound;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Nutrition.Components
{
    [RegisterComponent, Friend(typeof(SliceableFoodSystem))]
    internal class SliceableFoodComponent : Component
    {
        public override string Name => "SliceableFood";

        [DataField("slice")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string Slice = string.Empty;

        [DataField("sound")]
        [ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Items/Culinary/chop.ogg");

        [DataField("count")]
        [ViewVariables(VVAccess.ReadWrite)]
        public ushort TotalCount = 5;

        [ViewVariables(VVAccess.ReadWrite)]
        public ushort Count;
    }
}
