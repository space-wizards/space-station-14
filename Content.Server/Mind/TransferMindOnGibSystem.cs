using System.Linq;
using Content.Server.Body.Components;
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
        SubscribeLocalEvent<TransferMindOnGibComponent, BeingGibbedEvent>(OnGib);
    }

    private void OnGib(EntityUid uid, TransferMindOnGibComponent component, BeingGibbedEvent args)
    {
        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            return;

        var validParts = args.GibbedParts.Where(p => _tag.HasTag(p, component.TargetTag)).ToHashSet();
        if (!validParts.Any())
            return;

        var ent = _random.Pick(validParts);
        _mindSystem.TransferTo(mindId, ent, mind: mind);
    }
}
