using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Events;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Content.Server.Ladder;
using Content.Server.Popups;
using Content.Shared.Popups;

namespace Content.Shared.Ladder;

public sealed class LadderSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LadderdownComponent, StepTriggeredOffEvent>(OnStepTriggeredDown);
        SubscribeLocalEvent<LadderupComponent, StepTriggeredOffEvent>(OnStepTriggeredUp);
        SubscribeLocalEvent<LadderdownComponent, StepTriggerAttemptEvent>(OnStepTriggerAttemptDown);
        SubscribeLocalEvent<LadderupComponent, StepTriggerAttemptEvent>(OnStepTriggerAttemptUp);
    }

    private void OnStepTriggeredDown(EntityUid owner, LadderdownComponent component, ref StepTriggeredOffEvent args)
    {
       if (HasComp<GhostComponent>(owner))
           {
            return;
           }

          if (HasComp<HandsComponent>(owner))
            {
           var destination = EntityManager.EntityQuery<LadderupComponent>().FirstOrDefault();
            if (destination != null)
            {
                Transform(owner).Coordinates = Transform(destination.Owner).Coordinates;
                 _popup.PopupEntity(Loc.GetString("ladder-down"), owner, PopupType.Medium);
            }
            }
    }

     private void OnStepTriggeredUp(EntityUid uid, LadderupComponent component, ref StepTriggeredOffEvent args)
    {
       if (HasComp<GhostComponent>(owner))
         {
            return;
         }

        if (HasComp<HandsComponent>(owner))
            {
       var destination = EntityManager.EntityQuery<LadderdownComponent>().FirstOrDefault();
            if (destination != null)
            {
                Transform(owner).Coordinates = Transform(destination.Owner).Coordinates;
                _popup.PopupEntity(Loc.GetString("ladder-up"), owner, PopupType.Medium);
            }
            }
    }


    private void OnStepTriggerAttemptDown(EntityUid uid, LadderdownComponent component, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }

     private void OnStepTriggerAttemptUp(EntityUid uid, LadderupComponent component, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }
    
}
