using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.StepTrigger.Systems;
using Robust.Server.GameObjects;

namespace Content.Server.Tiles;

public sealed class LavaSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;

    public override void Initialize()
    {
        base.Initialize();
        // My brother in christ climbing code why are you like this.
        SubscribeLocalEvent<LavaComponent, StepTriggeredEvent>(OnLavaStepTriggered);
        SubscribeLocalEvent<LavaComponent, StepTriggerAttemptEvent>(OnLavaStepTriggerAttempt);
    }

    private void OnLavaStepTriggerAttempt(EntityUid uid, LavaComponent component, ref StepTriggerAttemptEvent args)
    {
        if (!HasComp<FlammableComponent>(args.Tripper) && !HasComp<LavaDisintegrationComponent>(args.Tripper))
            return;

        args.Continue = true;
    }

    private void OnLavaStepTriggered(EntityUid uid, LavaComponent component, ref StepTriggeredEvent args)
    {
        var otherUid = args.Tripper;

        if (HasComp<LavaDisintegrationComponent>(otherUid))
        {
            QueueDel(otherUid);
            _audio.PlayPvs(component.DisintegrationSound, uid);
            return;
        }
        if (TryComp<FlammableComponent>(otherUid, out var flammable))
        {
            // Apply the fury of a thousand suns
            var multiplier = flammable.FireStacks == 0f ? 5f : 1f;
            _flammable.AdjustFireStacks(otherUid, component.FireStacks * multiplier, flammable);
            _flammable.Ignite(otherUid, flammable);
        }
    }
}
