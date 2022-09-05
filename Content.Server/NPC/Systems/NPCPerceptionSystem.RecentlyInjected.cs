using Content.Server.NPC.Components;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCPerceptionSystem
{
    /// <summary>
    /// Tracks targets recently injected by medibots.
    /// </summary>
    /// <param name="frameTime"></param>
    private void UpdateRecentlyInjected(float frameTime)
    {
        foreach (var entity in EntityQuery<NPCRecentlyInjectedComponent>())
        {
            entity.Accumulator += frameTime;
            if (entity.Accumulator < entity.RemoveTime.TotalSeconds)
                continue;
            entity.Accumulator = 0;

            RemComp<NPCRecentlyInjectedComponent>(entity.Owner);
        }
    }
}
