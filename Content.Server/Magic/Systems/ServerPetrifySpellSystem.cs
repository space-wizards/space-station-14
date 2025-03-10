using Content.Server.Polymorph.Systems;
using Content.Shared.Magic.Components;
using Content.Shared.Magic.Systems;

namespace Content.Server.Magic.Systems;

public sealed class ServerPetrifySpellSystem : PetrifySpellSystem
{
    [Dependency] private readonly PolymorphSystem _poly = default!;

    protected override void OnPetrify(Entity<PetrifiedComponent> ent, ref MapInitEvent args)
    {
        _poly.PolymorphEntity(ent, ent.Comp.PolymorphPrototypeName, false, false);

        base.OnPetrify(ent, ref args);
    }
}
