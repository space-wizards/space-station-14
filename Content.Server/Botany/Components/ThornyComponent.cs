using Robust.Shared.Containers;
using Content.Shared.Damage;

namespace Content.Server.Botany.Components;
[RegisterComponent]
public sealed partial class ThornyComponent : Component
{
    [DataField]
    public string Sound = string.Empty;

    [DataField]
    public int ThrowStrength = 3;

    [DataField("damage")]
    public DamageSpecifier Damage = new();
}
