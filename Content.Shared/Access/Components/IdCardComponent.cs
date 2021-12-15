using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Access
{
    // TODO BUI NETWORKING if ever clients can open their own BUI's (id card console, pda), then this data should be
    // networked.
    [RegisterComponent]
    public class IdCardComponent : Component
    {
        public override string Name => "IdCard";

        [DataField("originalOwnerName")]
        public string OriginalOwnerName = default!;

        [DataField("fullName")]
        public string? FullName;

        [DataField("jobTitle")]
        public string? JobTitle;
    }
}
