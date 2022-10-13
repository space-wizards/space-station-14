using Content.Server.Interaction;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.CombatMode;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.NPC.Systems;

/// <summary>
/// Handles combat for NPCs.
/// </summary>
public sealed partial class NPCCombatSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly NPCSteeringSystem _steering = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

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
