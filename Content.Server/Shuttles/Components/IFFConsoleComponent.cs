using Content.Server.Shuttles.Systems;
using Content.Shared.Shuttles.Components;

namespace Content.Server.Shuttles.Components;

[RegisterComponent, Access(typeof(ShuttleSystem))]
public sealed partial class IFFConsoleComponent : Component
{
    /// <summary>
    /// Flags that this console is allowed to set.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("allowedFlags")]
    public IFFFlags AllowedFlags = IFFFlags.HideLabel;

    /// <summary>
    /// If true, automatically applies all supported IFF flags to the console's grid on MapInitEvent.
    /// </summary>
    [DataField]
    public bool HideOnInit = false;
}
