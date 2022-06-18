using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Sound;

namespace Content.Server.Nutrition.Components
{
    [RegisterComponent, Access(typeof(SliceableFoodSystem))]
    internal sealed class SliceableFoodComponent : Component
    {
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
