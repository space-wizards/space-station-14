using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Construction;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
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
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Tools.Systems;

public sealed class ToolRefinableSystem : EntitySystem
{
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToolRefinableComponent, GetVerbsEvent<InteractionVerb>>(AddVerb);
        SubscribeLocalEvent<ToolRefinableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ToolRefinableComponent, WelderRefineDoAfterEvent>(OnDoAfter);
    }

    private void OnInteractUsing(Entity<ToolRefinableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var (uid, component) = ent;
        var getIsBlocked = new GetIsToolRefineBlockedEvent(args.Used, component.QualityNeeded);
        RaiseLocalEvent(ref getIsBlocked);
        if (!getIsBlocked.IsRefinable)
        {
            _popup.PopupPredicted(getIsBlocked.BlockCause, args.User, args.User);
            return;
        }

        args.Handled = UseTool(uid, component, args.Used, args.User);
    }

    private void OnDoAfter(Entity<ToolRefinableComponent> ent, ref WelderRefineDoAfterEvent args)
    {
        if (args.Cancelled || args.Used == null)
            return;

        var (uid, component) = ent;

        var getIsBlocked = new GetIsToolRefineBlockedEvent(args.Used.Value, component.QualityNeeded);
        RaiseLocalEvent(ref getIsBlocked);
        if (!getIsBlocked.IsRefinable)
        {
            _popup.PopupPredicted(getIsBlocked.BlockCause, args.User, args.User);
            return;
        }

        PopupOnRefine(uid, component, args.Used.Value, args.User);

        // TODO: Use RandomPredicted https://github.com/space-wizards/RobustToolbox/pull/5849
        var rndSeed = SharedRandomExtensions.HashCodeCombine((int)_gameTiming.CurTick.Value, args.User.Id, uid.Id);
        var rng = new System.Random(rndSeed);

        var spawns = EntitySpawnCollection.GetSpawns(component.RefineResult, rng);
        List<EntityUid> spawned = new(spawns.Count);
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

        var ev = new BeforeToolRefineFinishedEvent(args.User);
        RaiseLocalEvent(uid, ref ev);

        if(ev.Cancelled)
            return;

        _body.GibBody(uid);
        _destructible.DestroyEntity(uid);
    }

    private void PopupOnRefine(EntityUid uid, ToolRefinableComponent component, EntityUid used, EntityUid user)
    {
        var slicingDoneMessageForUser = component.PopupForUser == null
            ? null
            : Loc.GetString(
                component.PopupForUser,
                ("target", uid),
                ("tool", used)
            );
        var slicingDoneMessageForOthers = component.PopupForOther == null
            ? null
            : Loc.GetString(
                component.PopupForOther,
                ("user", user),
                ("target", uid),
                ("tool", used)
            );
        var popupType = component.IsUsingCautionPopup
            ? PopupType.MediumCaution
            : PopupType.Small;

        _popup.PopupPredicted(slicingDoneMessageForUser, slicingDoneMessageForOthers, user, user, popupType);
    }

    private bool TryGetSourceSolutionForTransfer(
        EntityUid source,
        string solutionName,
        [NotNullWhen(true)] out (Entity<SolutionComponent> sourceSoln, Solution sourceSolution)? solutionInfo
    )
    {
        solutionInfo = default;

        if (!_solutionContainer.TryGetSolution(source, solutionName, out var sourceSoln, out var sourceSolution))
        {
            return false;
        }

        solutionInfo = (sourceSoln.Value, sourceSolution);
        return true;
    }

    private void FillResult(EntityUid sliceUid, string solutionName, Solution solution)
    {
        if (_solutionContainer.TryGetSolution(sliceUid, solutionName, out var itsSoln, out var itsSolution))
        {
            _solutionContainer.RemoveAllSolution(itsSoln.Value);

            var lostSolutionPart = solution.SplitSolution(itsSolution.AvailableVolume);
            _solutionContainer.TryAddSolution(itsSoln.Value, lostSolutionPart);
        }
    }

    private void AddVerb(Entity<ToolRefinableComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        var used = args.Using;
        if (ent.Comp.VerbText == null || !args.CanInteract || !args.CanAccess || !used.HasValue)
            return;

        var user = args.User;

        EntityUid? tool;
        string? verbMessage = null;
        bool verbDisabled = false;
        var comp = ent.Comp;

        if (args.Using is not null)
        {
            if (!_toolSystem.HasQuality(used.Value, comp.QualityNeeded))
            {
                verbDisabled = true;
                verbMessage = comp.ToolMissingQualityTooltip == null
                    ? null
                    : Loc.GetString(comp.ToolMissingQualityTooltip, ("target", ent.Owner));
            }
            tool = used;
        }
        else if (_toolSystem.HasQuality(user, comp.QualityNeeded))
        {
            tool = user;
        }
        else
        {
            return;
        }

        var getIsBlocked = new GetIsToolRefineBlockedEvent(used.Value, ent.Comp.QualityNeeded);
        RaiseLocalEvent(ref getIsBlocked);

        if (!getIsBlocked.IsRefinable)
        {
            verbDisabled = true;
            verbMessage = getIsBlocked.BlockCause;
        }

        verbMessage ??= comp.VerbDefaultTooltip == null
            ? null
            : Loc.GetString(comp.VerbDefaultTooltip.Value);

        InteractionVerb verb = new()
        {
            Text = Loc.GetString(ent.Comp.VerbText),
            Icon = ent.Comp.VerbIcon,
            Disabled = verbDisabled,
            Message = verbMessage,
            Act = () =>
            {
                if (tool.HasValue)
                    UseTool(ent, ent, used.Value, user);
            },
        };
        args.Verbs.Add(verb);
    }

    private bool UseTool(EntityUid uid, ToolRefinableComponent component, EntityUid used, EntityUid user)
    {
        return _toolSystem.UseTool(
            used,
            user,
            uid,
            component.RefineTime,
            [component.QualityNeeded],
            new WelderRefineDoAfterEvent(),
            out _,
            fuel: component.RefineFuel
        );
    }
}

/// <summary>
/// Event for checking if tool refining of entity is blocked in some complex way.
/// </summary>
[ByRefEvent]
public record struct GetIsToolRefineBlockedEvent(
    EntityUid Using,
    ProtoId<ToolQualityPrototype> RequiredToolQuality,
    bool IsRefinable = true,
    string? BlockCause = null
);

/// <summary>
/// Called after slicing of the entity.
/// </summary>
[ByRefEvent]
public record struct BeforeToolRefineFinishedEvent(EntityUid User)
{
    public bool Cancelled;
}
