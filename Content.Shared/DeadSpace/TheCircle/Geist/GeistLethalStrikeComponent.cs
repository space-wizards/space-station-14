// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Damage;

namespace Content.Shared.DeadSpace.TheCircle.Geist;

[RegisterComponent]
public sealed partial class GeistLethalStrikeComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(120);

    [DataField]
    public bool Armed;

    [DataField]
    public TimeSpan NextReady;
}
