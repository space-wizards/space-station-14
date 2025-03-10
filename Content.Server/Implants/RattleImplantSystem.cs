using Content.Server.Explosion.EntitySystems;
using Content.Server.Pinpointer;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Implants.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Implants;

public sealed class RattleImplantSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly RadioSystem _radioSystem = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RattleComponent, TriggerEvent>(HandleRattleTrigger);
    }

    private void HandleRattleTrigger(EntityUid uid, RattleComponent component, TriggerEvent args)
    {
        if (!TryComp<SubdermalImplantComponent>(uid, out var implanted))
            return;

        if (implanted.ImplantedEntity == null)
            return;

        // Gets location of the implant
        var posText = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(uid));
        var critMessage = Loc.GetString(component.CritMessage, ("user", implanted.ImplantedEntity.Value), ("position", posText));
        var deathMessage = Loc.GetString(component.DeathMessage, ("user", implanted.ImplantedEntity.Value), ("position", posText));

        if (!TryComp<MobStateComponent>(implanted.ImplantedEntity, out var mobstate))
            return;

        // Sends a message to the radio channel specified by the implant
        if (mobstate.CurrentState == MobState.Critical)
            _radioSystem.SendRadioMessage(uid, critMessage, _prototypeManager.Index<RadioChannelPrototype>(component.RadioChannel), uid);
        if (mobstate.CurrentState == MobState.Dead)
            _radioSystem.SendRadioMessage(uid, deathMessage, _prototypeManager.Index<RadioChannelPrototype>(component.RadioChannel), uid);

        args.Handled = true;
    }
}
