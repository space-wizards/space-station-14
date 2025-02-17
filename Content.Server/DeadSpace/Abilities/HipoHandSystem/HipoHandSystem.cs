using Content.Shared.Interaction;
using Content.Server.DeadSpace.Abilities.Cocoon.Components;
using Content.Shared.DeadSpace.Abilities.HipoHand.Components;
using Content.Shared.Body.Components;
using Content.Server.Popups;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.Abilities.HipoHand;

public sealed partial class HipoHandSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HipoHandComponent, UserActivateInWorldEvent>(OnInteract);
        SubscribeLocalEvent<HipoHandComponent, RegenReagentEvent>(OnRegenReagent);
    }

    private void OnRegenReagent(EntityUid uid, HipoHandComponent component, RegenReagentEvent args)
    {
        if (component.CountReagent < component.MaxCountReagent)
            SetReagentCount(uid, component, component.CountRegen);

        component.TimeUntilRegenReagent = _timing.CurTime + TimeSpan.FromSeconds(component.DurationRegenReagent);
    }

    private void OnInteract(EntityUid uid, HipoHandComponent component, UserActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target == args.User)
            return;

        if (component.CountReagent <= 0)
            return;

        var target = args.Target;

        if (TryComp<CocoonComponent>(target, out var cocoonComponent))
        {
            if (cocoonComponent.Stomach.ContainedEntities.Count > 0)
            {
                var firstEntity = cocoonComponent.Stomach.ContainedEntities[0];
                target = firstEntity;
            }
        }

        if (!HasComp<BodyComponent>(target))
            return;

        if (!_solutionContainer.TryGetInjectableSolution(target, out var injectable, out _))
            return;

        _audio.PlayPvs(component.InjectSound, target);
        _solutionContainer.TryAddReagent(injectable.Value, component.Reagent, component.Quantity, out _);
        _popup.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), target, target);

        SetReagentCount(uid, component, (float)-component.Quantity);

        args.Handled = true;
    }

    private void SetReagentCount(EntityUid uid, HipoHandComponent component, float count)
    {
        component.CountReagent += count;
        _popup.PopupEntity(Loc.GetString("У вас есть ") + component.CountReagent.ToString() + Loc.GetString(" реагента"), uid, uid);
    }

}
