// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Chemistry.Components;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.WaterCooler;

public sealed class ToggleableSolutionTransferSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleableSolutionTransferComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ToggleableSolutionTransferComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ToggleableSolutionTransferComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ToggleableSolutionTransferComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ToggleableSolutionTransferComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
    }

    private void OnStartup(Entity<ToggleableSolutionTransferComponent> ent, ref ComponentStartup args)
    {
        UpdateMode(ent);
    }

    private void OnMapInit(Entity<ToggleableSolutionTransferComponent> ent, ref MapInitEvent args)
    {
        UpdateMode(ent);
    }

    private void OnAfterHandleState(Entity<ToggleableSolutionTransferComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateMode(ent);
    }

    private void OnGetVerbs(Entity<ToggleableSolutionTransferComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var isOutput = ent.Comp.Direction == SolutionTransferDirection.Output;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString(isOutput
                ? "water-cooler-verb-switch-to-intake"
                : "water-cooler-verb-switch-to-dispensing"),
            Act = () =>
            {
                ent.Comp.Direction = ent.Comp.Direction == SolutionTransferDirection.Output
                    ? SolutionTransferDirection.Input
                    : SolutionTransferDirection.Output;
                UpdateMode(ent);
                Dirty(ent);
            },
            Priority = 1,
        });
    }

    private void OnExamined(Entity<ToggleableSolutionTransferComponent> ent, ref ExaminedEvent args)
    {
        var directionText = ent.Comp.Direction switch
        {
            SolutionTransferDirection.Input => Loc.GetString("water-cooler-mode-intake"),
            SolutionTransferDirection.Output => Loc.GetString("water-cooler-mode-dispensing"),
            _ => string.Empty,
        };

        if (!string.IsNullOrEmpty(directionText))
            args.PushText(directionText);
    }

    private void UpdateMode(Entity<ToggleableSolutionTransferComponent> ent)
    {
        if (ent.Comp.Direction == SolutionTransferDirection.Input)
        {
            RemCompDeferred<DrainableSolutionComponent>(ent);
            var refillable = EnsureComp<RefillableSolutionComponent>(ent);
            refillable.Solution = ent.Comp.Solution;
            Dirty(ent, refillable);
        }
        else
        {
            RemCompDeferred<RefillableSolutionComponent>(ent);
            var drainable = EnsureComp<DrainableSolutionComponent>(ent);
            drainable.Solution = ent.Comp.Solution;
            Dirty(ent, drainable);
        }
    }
}
