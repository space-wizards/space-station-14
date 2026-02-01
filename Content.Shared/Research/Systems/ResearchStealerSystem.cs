using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Content.Shared.Research.Components;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Research.Systems;

public sealed class ResearchStealerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedNinjaGlovesSystem _gloves = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResearchStealerComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);
        SubscribeLocalEvent<ResearchStealerComponent, ResearchStealDoAfterEvent>(OnDoAfter);
    }

    /// <summary>
    /// Start do after for downloading techs from a r&d server.
    /// Will only try if there is at least 1 tech researched.
    /// </summary>
    private void OnBeforeInteractHand(Entity<ResearchStealerComponent> ent, ref BeforeInteractHandEvent args)
    {
        // TODO: generic event
        if (args.Handled || !_gloves.AbilityCheck(ent, args, out var target))
            return;

        // can only hack the server, not a random console
        if (!TryComp<TechnologyDatabaseComponent>(target, out var database) || HasComp<ResearchClientComponent>(target))
            return;

        args.Handled = true;

        // fail fast if theres no techs to steal right now
        if (database.UnlockedTechnologies.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("ninja-download-fail"), ent, ent);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, ent, ent.Comp.Delay, new ResearchStealDoAfterEvent(), target: target, used: ent, eventTarget: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            MovementThreshold = 0.5f,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(Entity<ResearchStealerComponent> ent, ref ResearchStealDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        var target = args.Target.Value;

        if (!TryComp<TechnologyDatabaseComponent>(target, out var database))
            return;

        var ev = new ResearchStolenEvent(ent, target, []);
        var count = _random.Next(ent.Comp.MinToSteal, ent.Comp.MaxToSteal + 1); // TODO move to RandomPredicted
        for (var i = 0; i < count; i++)
        {
            if (database.UnlockedTechnologies.Count == 0)
                break;

            var toRemove = _random.Pick(database.UnlockedTechnologies); // TODO move to RandomPredicted
            if (_research.TryRemoveTechnology((target, database), toRemove))
                ev.Techs.Add(toRemove);
        }
        RaiseLocalEvent(ent, ref ev);

        args.Handled = true;
    }
}

/// <summary>
/// Raised on the research stealer when the doafter completes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ResearchStealDoAfterEvent : SimpleDoAfterEvent { }

/// <summary>
/// Event raised on the user when research is stolen from a RND server.
/// Techs contains every technology id researched.
/// </summary>
[ByRefEvent]
public record struct ResearchStolenEvent(EntityUid Used, EntityUid Target, List<string> Techs);
