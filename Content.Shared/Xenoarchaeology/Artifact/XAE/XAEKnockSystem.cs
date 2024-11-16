using Content.Shared.Magic.Events;
using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

public sealed class XAEKnockSystem : BaseXAESystem<XAEKnockComponent>
{
    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEKnockComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var ev = new KnockSpellEvent
        {
            Performer = ent.Owner,
            Range = ent.Comp.KnockRange
        };
        RaiseLocalEvent(ev);
    }
}
