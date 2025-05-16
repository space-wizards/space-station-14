using Content.Server.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Server.Body.Systems;

/// <summary>
/// Listens for the BloodColorOverrideEvent
/// so it can provide a color to slime blood.
/// </summary>
public sealed class SlimeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeBloodComponent, BloodColorOverrideEvent>(OnBloodColorOverride);
    }

    private void OnBloodColorOverride(Entity<SlimeBloodComponent> uid, ref BloodColorOverrideEvent args)
    {
        if (null == args.BloodstreamComp)
            return;
        var bloodProto = _prototypeManager.Index<ReagentPrototype>(args.BloodstreamComp.BloodReagent);
        if ("Slime" == bloodProto.ID
            && TryComp<HumanoidAppearanceComponent>(uid, out var appearanceComp))
        {
            args.BloodstreamComp.BloodOverrideColor = appearanceComp.SkinColor;
        }
    }
}
