using Content.Server.Ghost.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Polymorph.Systems;
using Content.Shared.Magic.Components;
using Content.Shared.Magic.Systems;

namespace Content.Server.Magic.Systems;

public sealed class ServerPetrifySpellSystem : PetrifySpellSystem
{
    [Dependency] private readonly PolymorphSystem _poly = default!;
    [Dependency] private readonly NPCSystem _npc = default!;

    protected override void OnPetrify(Entity<PetrifiedComponent> ent, ref MapInitEvent args)
    {
        _poly.PolymorphEntity(ent, ent.Comp.PolymorphPrototypeName, false, false);

        base.OnPetrify(ent, ref args);
    }

    protected override void OnAnimate(Entity<PetrifiedStatueComponent> ent, ref AnimateSpellEvent args)
    {
        base.OnAnimate(ent, ref args);

        _poly.PolymorphEntity(ent, ent.Comp.PolymorphPrototypeName, true, false);

        //RemComp<GhostOnMoveComponent>(ent);
        //EnsureComp<HTNComponent>(ent);
        //_npc.SleepNPC(ent);
    }
}
