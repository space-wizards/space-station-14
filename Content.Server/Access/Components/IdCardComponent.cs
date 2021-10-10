using Content.Server.Access.Systems;
using Content.Server.PDA;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Access.Components
{
    [RegisterComponent]
    [Friend(typeof(IdCardSystem), typeof(PDASystem))]
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
