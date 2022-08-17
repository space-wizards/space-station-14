using Content.Server.Interaction;
using Robust.Shared.Timing;

namespace Content.Server.NPC.Systems;

/// <summary>
/// Handles combat for NPCs.
/// </summary>
public sealed partial class NPCCombatSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeMelee();
        InitializeRanged();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateMelee(frameTime);
        UpdateRanged(frameTime);
    }
}
