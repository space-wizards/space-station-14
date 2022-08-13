using Robust.Shared.Utility;

namespace Content.Shared.CartridgeComputer;


[RegisterComponent]
public sealed class CartridgeComponent : Component
{
    [DataField("programName", required: true)]
    public string ProgramName = string.Empty;

    [DataField("icon")]
    public SpriteSpecifier? Icon;
}
