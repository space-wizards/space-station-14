// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using Content.Server.Fluids.EntitySystems;
using Content.Shared.BloodCult;
using Content.Shared.BloodCult.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.BloodCult.EntityEffects;

/// <summary>
/// When blood is splashed on a juggernaut, creates Unholy Blood puddles on the ground.
/// This represents the blood being corrupted by the construct's unholy essence.
/// </summary>
public sealed partial class JuggernautBloodCorruption : EntityEffectBase<JuggernautBloodCorruption>
{
    [DataField]
    public ProtoId<ReagentPrototype> CorruptedReagent = "UnholyBlood";

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-juggernaut-blood-corruption", ("chance", Probability));
}

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class JuggernautBloodCorruptionEntityEffectSystem : EntityEffectSystem<JuggernautComponent, JuggernautBloodCorruption>
{
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<JuggernautComponent> entity, ref EntityEffectEvent<JuggernautBloodCorruption> args)
    {
        // Scale represents the reagent quantity
        if (args.Scale <= 0)
            return;

        // Create a solution of Unholy Blood with the same volume as the reagent quantity that was applied
        var corruptedSolution = new Solution();
        corruptedSolution.AddReagent(args.Effect.CorruptedReagent, FixedPoint2.New(args.Scale));

        // Spawn a puddle at the juggernaut's feet
        var coordinates = _transform.GetMoverCoordinates(entity);
        _puddle.TrySpillAt(coordinates, corruptedSolution, out _, sound: false);
    }
}
