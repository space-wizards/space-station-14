using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._Starlight.Railroading;

[RegisterComponent]
public sealed partial class RuleOwnerComponent : Component
{
    [DataField]
    public EntityUid RuleOwner;
}