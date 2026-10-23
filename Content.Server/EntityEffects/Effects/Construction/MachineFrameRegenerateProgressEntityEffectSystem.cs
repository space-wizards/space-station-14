using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Construction;

namespace Content.Server.EntityEffects.Effects.Construction;

public sealed partial class MachineFrameRegenerateProgressEntityEffectSystem : EntityEffectSystem<MachineFrameComponent, MachineFrameRegenerateProgress>
{
    [Dependency] private MachineFrameSystem _machine = default!;

    protected override void Effect(Entity<MachineFrameComponent> entity, ref EntityEffectEvent<MachineFrameRegenerateProgress> args)
    {
        _machine.RegenerateProgress(entity.Comp);
    }
}
