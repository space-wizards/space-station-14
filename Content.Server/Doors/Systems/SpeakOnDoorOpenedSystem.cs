using Content.Server.Chat.Systems;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Doors.Systems;

public sealed partial class SpeakOnDoorOpenedSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeakOnDoorOpenedComponent, DoorStateChangedEvent>(OnDoorStateChanged);
    }
    private void OnDoorStateChanged(Entity<SpeakOnDoorOpenedComponent> ent, ref DoorStateChangedEvent args)
    {
        if (args.State != DoorState.Open)
            return;

        if (ent.Comp.NeedsPower && !_power.IsPowered(ent.Owner))
            return;

        if (!_random.Prob(ent.Comp.Probability))
            return;

        if (!_prototypeManager.TryIndex(ent.Comp.Pack, out var messagePack))
            return;

        var message = Loc.GetString(_random.Pick(messagePack.Values));
        _chat.TrySendInGameICMessage(ent.Owner, message, InGameICChatType.Speak, true);
    }
}
