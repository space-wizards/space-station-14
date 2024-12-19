using Content.Shared.Magic.Events;
using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

public sealed class XAEKnockSystem : BaseXAESystem<XAEKnockComponent>
{
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEKnockComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var ev = new KnockSpellEvent
        {
            Performer = ent.Owner,
            Range = ent.Comp.KnockRange
        };
        RaiseLocalEvent(ev);
    }
}
