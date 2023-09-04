using System.Linq;
using Content.Server.Body.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Tag;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;

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
    [Dependency] private readonly ISerializationManager _serializationManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TransferMindOnGibComponent, BeingGibbedEvent>(OnGib);
    }

    private void OnGib(EntityUid uid, TransferMindOnGibComponent comp, BeingGibbedEvent args)
    {
        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            return;

        var validParts = args.GibbedParts.Where(p => _tag.HasTag(p, comp.TargetTag)).ToHashSet();
        if (!validParts.Any())
            return;

        var ent = _random.Pick(validParts);

        foreach (var component in comp.TransferredComponents.Values)
        {
            var typeComp = component.Component.GetType();
            if (!HasComp(uid, typeComp) || HasComp(ent, typeComp))
                continue;

            var newComp = (Component) _serializationManager.CreateCopy(component.Component, notNullableOverride: true);
            EntityManager.AddComponent(ent, newComp);
        }

        _mindSystem.TransferTo(mindId, ent, mind: mind);
    }
}
