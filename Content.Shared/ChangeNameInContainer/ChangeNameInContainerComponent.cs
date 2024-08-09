using Content.Shared.Whitelist;

namespace Content.Shared.ChangeNameInContainer;

[RegisterComponent]
public sealed partial class ChangeNameInContainerComponent : Component
{
    /// <summary>
    ///     Whitelist for what entities are allowed in the suit storage slot.
    /// </summary>
    [DataField]
    public EntityWhitelist Whitelist = new()
    {
        Components = new[] {"Item"}
    };
}
