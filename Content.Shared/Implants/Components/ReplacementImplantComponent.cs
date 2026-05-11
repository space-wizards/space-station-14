using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Implants.Components;

/// <summary>
/// Added to implants with the see <see cref="SubdermalImplantComponent"/>.
/// When implanted it will cause other implants in the whitelist to be deleted and thus replaced.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ReplacementImplantComponent : Component
{
    /// <summary>
    /// Whitelist for which implants to delete.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();
}
