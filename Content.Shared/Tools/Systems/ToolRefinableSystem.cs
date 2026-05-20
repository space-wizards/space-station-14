using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Construction;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Gibbing;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Storage;
using Content.Shared.Tools.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Tools.Systems;

public sealed partial class ToolRefinablSystem : EntitySystem
{
    [Dependency] private SharedToolSystem _toolSystem = default!;
    [Dependency] private GibbingSystem _gib = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedDestructibleSystem _destructible = default!;
    [Dependency] private IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToolRefinableComponent, GetVerbsEvent<InteractionVerb>>(AddVerb);
        SubscribeLocalEvent<ToolRefinableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ToolRefinableComponent, ToolRefineDoAfterEvent>(OnDoAfter);
    }

    #region Subscriptions

    private void OnInteractUsing(Entity<ToolRefinableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var component = ent.Comp;
        var uid = ent.Owner;
        var getIsBlocked = new AttemptToolRefineEvent(args.Used);
        RaiseLocalEvent(ref getIsBlocked);
        if (!getIsBlocked.IsCancelled)
        {
            _popup.PopupPredicted(getIsBlocked.BlockCause, args.User, args.User);
            return;
        }

        args.Handled = UseTool(uid, component, args.Used, args.User);
    }

    private void AddVerb(Entity<ToolRefinableComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        var used = args.Using;
        var component = ent.Comp;

        if (ent.Comp.VerbText == null || !args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;

        var tool = used ?? user;
        var verbDisabled = false;
        string? verbMessage = null;

        if (!_toolSystem.HasQuality(tool, ent.Comp.QualityNeeded))
        {
            verbDisabled = true;
            verbMessage = ent.Comp.ToolMissingQualityTooltip == null
                ? null
                : Loc.GetString(ent.Comp.ToolMissingQualityTooltip, ("target", ent.Owner));
        }

        var getIsBlocked = new AttemptToolRefineEvent(tool);
        RaiseLocalEvent(ref getIsBlocked);

        if (!getIsBlocked.IsCancelled)
        {
            verbDisabled = true;
            verbMessage = getIsBlocked.BlockCause;
        }

        verbMessage ??= component.VerbDefaultTooltip == null
            ? null
            : Loc.GetString(component.VerbDefaultTooltip.Value);

        InteractionVerb verb = new()
        {
            Text = Loc.GetString(ent.Comp.VerbText),
            Icon = ent.Comp.VerbIcon,
            Disabled = verbDisabled,
            Message = verbMessage,
            Act = () =>
            {
                if (!verbDisabled)
                    UseTool(ent, ent, tool, user);
            },
        };
        args.Verbs.Add(verb);
    }

    private void OnDoAfter(Entity<ToolRefinableComponent> ent, ref ToolRefineDoAfterEvent args)
    {
        if (args.Cancelled || args.Used == null)
            return;

        var component = ent.Comp;
        var uid = ent.Owner;

        var getIsBlocked = new AttemptToolRefineEvent(args.Used.Value);
        RaiseLocalEvent(ref getIsBlocked);
        if (!getIsBlocked.IsCancelled)
        {
            _popup.PopupPredicted(getIsBlocked.BlockCause, args.User, args.User);
            return;
        }

        PopupOnRefine(ent, args.Used.Value, args.User);

        var ev = new BeforeToolRefinedEvent(args.User);
        RaiseLocalEvent(uid, ref ev);

        if (ev.Cancelled)
            return;

        // TODO: Use RandomPredicted https://github.com/space-wizards/RobustToolbox/pull/5849
        var rndSeed = SharedRandomExtensions.HashCodeCombine((int)_gameTiming.CurTick.Value, args.User.Id, uid.Id);
        var rng = new System.Random(rndSeed);

        var spawns = EntitySpawnCollection.GetSpawns(component.RefineResult, rng);
        var spawned = new List<EntityUid>(spawns.Count);
        foreach (var protoId in spawns)
        {
            var sliceUid = PredictedSpawnNextToOrDrop(protoId, uid);
            spawned.Add(sliceUid);

            if (!_container.IsEntityOrParentInContainer(sliceUid))
            {
                var randVect = rng.NextPolarVector2(2.0f, 2.5f);
                _physics.SetLinearVelocity(sliceUid, randVect);
            }
        }

        if (TryComp<ToolRefinableSolutionComponent>(uid, out var comp))
        {
            TryGetSourceSolutionForTransfer(uid, comp.SolutionToSplit, out var solutionInfo);

            foreach (var spawnedUid in spawned)
            {
                // Fills new slice if original entity allows.
                if (solutionInfo.HasValue && comp.SolutionToSet != null)
                {
                    var (sourceSoln, sourceSolution) = solutionInfo.Value;
                    var sliceVolume = sourceSolution.Volume / FixedPoint2.New(spawns.Count);

                    var lostSolution = _solutionContainer.SplitSolution(sourceSoln, sliceVolume);
                    FillResult(spawnedUid, comp.SolutionToSet, lostSolution);
                }
            }
        }

        if (component.Sound != null)
            _audio.PlayPredicted(component.Sound, Transform(uid).Coordinates, args.User, AudioParams.Default.WithVolume(-2));

        _gib.Gib(uid);
        _destructible.DestroyEntity(uid);
    }

    #endregion

    private void PopupOnRefine(Entity<ToolRefinableComponent> ent, EntityUid used, EntityUid user)
    {
        var component = ent.Comp;
        var uid = ent.Owner;

        var slicingDoneMessageForUser = component.PopupForUser == null
            ? null
            : Loc.GetString(component.PopupForUser, ("target", uid), ("tool", used));
        var slicingDoneMessageForOthers = component.PopupForOther == null
            ? null
            : Loc.GetString(component.PopupForOther, ("user", user), ("target", uid), ("tool", used));

        _popup.PopupPredicted(slicingDoneMessageForUser, slicingDoneMessageForOthers, user, user, component.PopupType);
    }

    private bool TryGetSourceSolutionForTransfer(
        EntityUid source,
        string solutionName,
        [NotNullWhen(true)] out (Entity<SolutionComponent> sourceSoln, Solution sourceSolution)? solutionInfo
    )
    {
        solutionInfo = default;

        if (!_solutionContainer.TryGetSolution(source, solutionName, out var sourceSoln, out var sourceSolution))
            return false;

        solutionInfo = (sourceSoln.Value, sourceSolution);
        return true;
    }

    private void FillResult(EntityUid sliceUid, string solutionName, Solution solution)
    {
        if (!_solutionContainer.TryGetSolution(sliceUid, solutionName, out var itsSoln, out var itsSolution))
            return;

        _solutionContainer.RemoveAllSolution(itsSoln.Value);

        var lostSolutionPart = solution.SplitSolution(itsSolution.AvailableVolume);
        _solutionContainer.TryAddSolution(itsSoln.Value, lostSolutionPart);
    }

    private bool UseTool(EntityUid uid, ToolRefinableComponent component, EntityUid used, EntityUid user)
    {
        return _toolSystem.UseTool(
            used,
            user,
            uid,
            component.RefineTime,
            [component.QualityNeeded],
            new ToolRefineDoAfterEvent(),
            out _,
            fuel: component.RefineFuel
        );
    }
}

/// <summary>
/// Event for checking if tool refining of entity is blocked in some complex way.
/// </summary>
[ByRefEvent]
public record struct AttemptToolRefineEvent(
    EntityUid Using,
    bool IsCancelled = true,
    string? BlockCause = null
);

/// <summary>
/// Called after slicing of the entity.
/// </summary>
[ByRefEvent]
public record struct BeforeToolRefinedEvent(EntityUid User)
{
    public bool Cancelled;
}
