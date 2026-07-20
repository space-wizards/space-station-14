using Content.Shared.Speech.Components;

namespace Content.Server.Speech.Components;

/// <summary>
///     Nyehh, my gabagool, see?
///     Etc etc.
/// </summary>
[RegisterComponent]
public sealed partial class MobsterAccentComponent : BaseAccentComponent
{
    /// <summary>
    ///     Do you make all the rules?
    /// </summary>
    [DataField("isBoss")]
    public bool IsBoss = true;
}
