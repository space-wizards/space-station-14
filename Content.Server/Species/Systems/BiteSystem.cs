// SPDX-FileCopyrightText: 2025 Drywink <hugogrethen@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Species;
using Robust.Shared.Player;

namespace Content.Server.Species;

public sealed class BiteSystem : SharedBiteSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BiteComponent, BiteActionEvent>(OnBiteAction);
    }

    private void OnBiteAction(EntityUid uid, BiteComponent component, BiteActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;
        var user = args.Performer;

        // Check if target has an injectable solution
        if (!_solution.TryGetInjectableSolution(target, out var targetSolution, out var _))
        {
            _popup.PopupEntity(Loc.GetString("bite-component-cannot-bite-message",
                ("target", Identity.Entity(target, EntityManager))), user, user);
            return;
        }

        // Create solution from configured reagents
        var solution = new Solution();
        foreach (var (reagentId, amount) in component.InjectedReagents)
        {
            solution.AddReagent(reagentId, amount);
        }

        if (!_solution.TryAddSolution(targetSolution!.Value, solution))
        {
            _popup.PopupEntity(Loc.GetString("bite-component-cannot-inject-message",
                ("target", Identity.Entity(target, EntityManager))), user, user);
            return;
        }

        // Notify the user
        _popup.PopupEntity(Loc.GetString("bite-component-bite-success-message",
            ("target", Identity.Entity(target, EntityManager))), user, user);

        // Notify the target
        _popup.PopupEntity(Loc.GetString("bite-component-bitten-message",
            ("user", Identity.Entity(user, EntityManager))), target, target, PopupType.MediumCaution);

        args.Handled = true;
    }
}

