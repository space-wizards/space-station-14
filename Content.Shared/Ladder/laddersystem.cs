using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Events;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Content.Shared.Climbing.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Ghost;
using Content.Shared.Climbing.Components;
using Content.Shared.Climbing.Events;
using Robust.Shared.Random;

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

       // SubscribeLocalEvent<LadderdownComponent, StepTriggeredOffEvent>(OnStepTriggeredDown);
       // SubscribeLocalEvent<LadderupComponent, StepTriggeredOffEvent>(OnStepTriggeredUp);
        SubscribeLocalEvent<LadderdownComponent, ClimbedOnEvent>(OnClimbedDown);
        SubscribeLocalEvent<LadderupComponent, ClimbedOnEvent>(OnClimbedUp);
    }

    // Called when you are climbing down a ladder
    private void OnClimbedDown(EntityUid uid, LadderdownComponent component, ref ClimbedOnEvent args)
    {
          if (HasComp<HandsComponent>(args.Climber))
            {
             var destination = EntityManager.EntityQuery<LadderupComponent>().FirstOrDefault(); // finds the nearest ladder
             if (destination != null)
                {
                Transform(args.Climber).Coordinates = Transform(destination.Owner).Coordinates;
                    // teleports you to the other ladder
                }
            }
    }

    // called when you are climbing up a ladder
     private void OnClimbedUp(EntityUid uid, LadderupComponent component, ref ClimbedOnEvent args)
    {
        if (HasComp<HandsComponent>(args.Climber))
            {
              var destination = EntityManager.EntityQuery<LadderdownComponent>().FirstOrDefault();
              if (destination != null)
                {
                Transform(args.Climber).Coordinates = Transform(destination.Owner).Coordinates;
                }
            }
    }

}


[RegisterComponent]
public sealed partial class LadderdownComponent : Component
{

}

[RegisterComponent]
public sealed partial class LadderupComponent : Component
{

}
