using Content.Server.Polymorph.Systems;
using Content.Shared.Magic.Components;
using Content.Shared.Magic.Systems;

namespace Content.Server.Magic.Systems;

public sealed class ServerPetrifySpellSystem : PetrifySpellSystem
{
    [Dependency] private readonly PolymorphSystem _poly = default!;

    protected override void OnPetrify(Entity<PetrifiedComponent> ent, ref MapInitEvent args)
    {
        _poly.PolymorphEntity(ent, ent.Comp.PolymorphPrototypeName);

        base.OnPetrify(ent, ref args);
    }

    protected override void OnAnimate(Entity<PetrifiedComponent> ent, ref AnimateSpellEvent args)
    {
        base.OnAnimate(ent, ref args);
    }
}
