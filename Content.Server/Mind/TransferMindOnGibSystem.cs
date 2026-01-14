using System.Linq;
using Content.Shared.Gibbing;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Tag;
using Robust.Shared.Random;

namespace Content.Server.Mind;

/// <summary>
/// This handles transfering a target's mind
/// to a different entity when they gib.
/// used for skeletons.
/// </summary>
public sealed class TransferMindOnGibSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TransferMindOnGibComponent, GibbedBeforeDeletionEvent>(OnGib);
    }

    private void OnGib(Entity<TransferMindOnGibComponent> ent, ref GibbedBeforeDeletionEvent args)
    {
        if (!_mindSystem.TryGetMind(ent, out var mindId, out var mind))
            return;

        var validParts = args.Giblets.Where(p => _tag.HasTag(p, ent.Comp.TargetTag)).ToHashSet();
        if (!validParts.Any())
            return;

        var transfer = _random.Pick(validParts);
        _mindSystem.TransferTo(mindId, transfer, mind: mind);
    }
}
