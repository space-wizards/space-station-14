using Content.Server.Administration.Logs;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Lube;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.Lube;

public sealed class LubeSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LubeComponent, AfterInteractEvent>(OnInteract, after: new[] { typeof(OpenableSystem) });
    }

    private void OnInteract(Entity<LubeComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach || args.Target is not { Valid: true } target)
            return;

        if (TryLube(entity, target, args.User))
        {
            args.Handled = true;
            _audio.PlayPvs(entity.Comp.Squeeze, entity);
            _popup.PopupEntity(Loc.GetString("lube-success", ("target", Identity.Entity(target, EntityManager))), args.User, args.User, PopupType.Medium);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("lube-failure", ("target", Identity.Entity(target, EntityManager))), args.User, args.User, PopupType.Medium);
        }
    }

    private bool TryLube(Entity<LubeComponent> lube, EntityUid target, EntityUid actor)
    {
        if (HasComp<LubedComponent>(target) || !HasComp<ItemComponent>(target))
        {
            return false;
        }

        if (HasComp<ItemComponent>(target) && _solutionContainer.TryGetSolution(lube.Owner, lube.Comp.Solution, out _, out var solution))
        {
            var quantity = solution.RemoveReagent(lube.Comp.Reagent, lube.Comp.Consumption);
            if (quantity > 0)
            {
                var lubed = EnsureComp<LubedComponent>(target);
                lubed.SlipsLeft = _random.Next(lube.Comp.MinSlips * quantity.Int(), lube.Comp.MaxSlips * quantity.Int());
                lubed.SlipStrength = lube.Comp.SlipStrength;
                _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(actor):actor} lubed {ToPrettyString(target):subject} with {ToPrettyString(lube.Owner):tool}");
                return true;
            }
        }
        return false;
    }
}
