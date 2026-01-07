using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Sliceable;

public sealed class SliceableSystem : EntitySystem
{
    private static readonly ProtoId<ToolQualityPrototype> SlicingToolQuality = "Slicing";

    [Dependency] private readonly MobStateSystem _mob = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateComponent, GetIsToolRefineBlockedEvent>(GetIsRefinable);
    }

    private void GetIsRefinable(Entity<MobStateComponent> ent, ref GetIsToolRefineBlockedEvent args)
    {
        if (args.RequiredToolQuality == SlicingToolQuality && !_mob.IsDead(ent, ent))
        {
            args = args with { IsRefinable = false, BlockCause = Loc.GetString("slice-verb-target-isnt-dead") };
        }
    }
}
