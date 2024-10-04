using Content.Server.Damage.Components;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
///     Makes the entity very fragile, enough to break when thrown. Will spill contents if this is a SolutionContainer.
/// </summary>
public sealed partial class Fragile : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        var damageable = args.EntityManager.EnsureComponent<DamageableComponent>(args.TargetEntity);

        var hurtOnThrow = args.EntityManager.EnsureComponent<DamageOnLandComponent>(args.TargetEntity);
        hurtOnThrow.Damage = new() { DamageDict = new() { { "Blunt", 5 } } };

        var destroy = args.EntityManager.EnsureComponent<DestructibleComponent>(args.TargetEntity);
        var dt = new DamageThreshold()
        {
            Trigger = new Destructible.Thresholds.Triggers.DamageTrigger() { Damage = 5 },
        };

        if (args.EntityManager.TryGetComponent<SolutionContainerManagerComponent>(args.TargetEntity, out var solutionContainerManager))
        {
            var toSpill = solutionContainerManager?.Containers?.FirstOrDefault();
            if (toSpill != null)
            {
                dt.Behaviors.Add(new SpillBehavior() { Solution = toSpill });
            }
        }

        dt.Behaviors.Add(new DoActsBehavior() { Acts = ThresholdActs.Destruction });
        dt.Behaviors.Add(new PlaySoundBehavior() { Sound = new SoundCollectionSpecifier("desecration") });
        destroy.Thresholds.Add(dt);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}
