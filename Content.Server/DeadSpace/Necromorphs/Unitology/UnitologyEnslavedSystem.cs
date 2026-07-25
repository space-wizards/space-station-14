// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Necromorphs.Unitology.Components;
using Content.Shared.DeadSpace.Necromorphs.Unitology;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Radio.Components;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Necromorphs.Unitology;

public sealed class UnitologyEnslavedSystem : EntitySystem
{
    private static readonly ProtoId<RadioChannelPrototype> UnitologyChannel = "Unitolog";

    [Dependency] private readonly UnitologyRuleSystem _unitologyRule = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _implant = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly RoleSystem _role = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnitologyEnslavedComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<UnitologyEnslavedComponent, UnitologyMindShieldAddedEvent>(OnMindShieldAdded);
    }

    private void OnComponentInit(EntityUid uid, UnitologyEnslavedComponent comp, ComponentInit args)
    {
        if (TryComp<ImplantedComponent>(uid, out var implanted))
        {
            foreach (var implant in implanted.ImplantContainer.ContainedEntities)
            {
                if (!HasComp<MindShieldImplantComponent>(implant))
                    continue;

                _implant.ForceRemove(uid, implant);
                break;
            }
        }

        RemComp<MindShieldComponent>(uid);
        _unitologyRule.TryGrantUnitologyRole(uid, UnitologyRuleSystem.EnslavedUnitologyAntagRole, forceCreateRule: false);
    }

    private void OnMindShieldAdded(EntityUid uid, UnitologyEnslavedComponent comp, ref UnitologyMindShieldAddedEvent args)
    {
        RemComp<UnitologyComponent>(uid);
        RemComp<UnitologyEnslavedComponent>(uid);
        RemoveUnitologyRadio(uid);

        if (_mind.TryGetMind(uid, out var mindId, out _))
            _role.MindRemoveRole(mindId, "MindRoleEnslavedUnitology");
    }

    private void RemoveUnitologyRadio(EntityUid uid)
    {
        if (TryComp<IntrinsicRadioTransmitterComponent>(uid, out var transmitter))
        {
            transmitter.Channels.Remove(UnitologyChannel);
            if (transmitter.Channels.Count == 0)
                RemCompDeferred<IntrinsicRadioTransmitterComponent>(uid);
        }

        if (TryComp<ActiveRadioComponent>(uid, out var active))
        {
            active.Channels.Remove(UnitologyChannel);
            if (active.Channels.Count == 0)
                RemCompDeferred<ActiveRadioComponent>(uid);
            else
                Dirty(uid, active);
        }

        RemCompDeferred<IntrinsicRadioReceiverComponent>(uid);
    }
}
