using Content.Shared.Changeling.Components;
using Content.Shared.Destructible;

namespace Content.Shared.Changeling.Systems;

public sealed partial class DestructionResistanceSystem : EntitySystem
{
    /// <summary>
    /// Prevent destruction via non-ashing sources, if appropriate.
    /// </summary>
    /// <param name="ent">The entity.</param>
    /// <param name="args">The destruction event args. Canceled if the component is set to disable gibbing.</param>
    [SubscribeLocalEvent]
    private void OnDestruction(Entity<DestructionResistanceComponent> ent, ref DestructionAttemptEvent args)
    {
        if (ent.Comp.Enabled)
            args.Cancel();
    }

    /// <summary>
    /// Enable or disable the DestructionResistanceComponent.
    /// </summary>
    /// <param name="ent">The entity that has the component.</param>
    /// <param name="enabled">If it is enabled or not.</param>
    public void SetEnabled(Entity<DestructionResistanceComponent?> ent, bool enabled)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Enabled = enabled;
        Dirty(ent);
    }
}
