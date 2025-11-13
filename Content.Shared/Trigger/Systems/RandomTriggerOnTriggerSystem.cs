using Content.Shared.Random.Helpers;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Trigger.Systems;

public sealed class RandomTriggerOnTriggerSystem : XOnTriggerSystem<RandomTriggerOnTriggerComponent>
{
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    protected override void OnTrigger(Entity<RandomTriggerOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        // TODO: Replace with RandomPredicted once the engine PR is merged
        var hash = new List<int>
        {
            (int)_timing.CurTick.Value,
            GetNetEntity(ent).Id,
            args.User == null ? 0 : GetNetEntity(args.User.Value).Id,
        };
        var seed = SharedRandomExtensions.HashCodeCombine(hash);
        var rand = new System.Random(seed);

        var keyOut = _prototypeManager.Index(ent.Comp.RandomKeyOut).Pick(rand);

        // Prevent recursive triggers
        if (target == ent.Owner && ent.Comp.KeysIn.Contains(keyOut))
        {
            Log.Warning($"{ToPrettyString(ent)} attempted to recursively trigger itself using RandomTriggerOnTriggerComponent.");
            return;
        }

        _trigger.Trigger(target, args.User, keyOut);
        args.Handled = true;
    }
}
