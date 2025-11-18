using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Storage;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Sliceable;

/// <inheritdoc cref="SliceableComponent"/>
public sealed class SliceableSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SliceableComponent, TrySliceDoAfterEvent>(AfterSlicing);
        SubscribeLocalEvent<SliceableComponent, GetVerbsEvent<InteractionVerb>>(AddSliceVerb);
        SubscribeLocalEvent<SliceableComponent, InteractUsingEvent>(OnInteraction);
    }

    private void OnInteraction(Entity<SliceableComponent> ent, ref InteractUsingEvent args)
    {
        SliceableComponent sliceableComp = ent;
        if (args.Handled || !_toolSystem.HasQuality(args.Used, sliceableComp.ToolQuality))
            return;

        args.Handled = true;
        CreateDoAfter(ent, args.User, args.Used, sliceableComp.SliceTime.Seconds, sliceableComp.ToolQuality);
    }

    private void AddSliceVerb(Entity<SliceableComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;
        if (!TryGetVerbDataAndTool(ent, ent, args, user, out var verbDisabled, out var verbMessage, out var tool))
            return;

        SliceableComponent sliceableComp = ent;
        InteractionVerb verb = new()
        {
            Text = Loc.GetString("slice-verb-name"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/cutlery.svg.192dpi.png")),
            Disabled = verbDisabled,
            Message = verbMessage ?? Loc.GetString("slice-verb-message-default"),
            Act = () =>
            {
                if(tool.HasValue)
                    CreateDoAfter(ent, user, tool.Value, sliceableComp.SliceTime.Seconds, sliceableComp.ToolQuality);
            },
        };
        args.Verbs.Add(verb);
    }

    private void CreateDoAfter(EntityUid uid, EntityUid user, EntityUid used, float time, string qualities)
    {
        _toolSystem.UseTool(used, user, uid, time, qualities, new TrySliceDoAfterEvent());
    }

    private void AfterSlicing(Entity<SliceableComponent> ent, ref TrySliceDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used is null)
            return;

        // only show a big popup when butchering living things.
        // Meant to differentiate cutting up clothes and cutting up your boss.
        var popupType = HasComp<MobStateComponent>(ent)
            ? PopupType.LargeCaution
            : PopupType.Small;

        var slicingDoneMessageForUser = Loc.GetString(
            "slice-butchered-success",
            ("target", ent.Owner),
            ("tool", args.Used.Value)
        );
        var slicingDoneMessageForOthers = Loc.GetString(
            "slice-butchered-success-others",
            ("user", args.User),
            ("target", ent.Owner),
            ("tool", args.Used.Value)
        );
        _popup.PopupPredicted(slicingDoneMessageForUser, slicingDoneMessageForOthers, args.User, args.User, popupType);
        if (TrySlice(ent, args.User))
        {
            var ev = new SliceEvent();
            RaiseLocalEvent(ent, ref ev);

            _body.GibBody(ent);
            _destructible.DestroyEntity(ent.Owner);
        }
    }

    private bool TrySlice(Entity<SliceableComponent> ent, EntityUid user)
    {
        var slices = EntitySpawnCollection.GetSpawns(ent.Comp.Slices);

        var rndSeed = SharedRandomExtensions.HashCodeCombine((int) _gameTiming.CurTick.Value, user.Id, ent.Owner.Id);
        var rng = new System.Random(rndSeed);

        (Entity<SolutionComponent> sourceSoln, Solution sourceSolution)? solutionInfo = default;
        if (ent.Comp.SolutionToSplit != null)
            TryGetSourceSolutionForTransfer(ent, ent.Comp.SolutionToSplit, out solutionInfo);

        foreach (var sliceProtoId in slices)
        {
            var sliceUid = PredictedSpawnNextToOrDrop(sliceProtoId, ent);
            if (slices.Count != 0 && !_container.IsEntityOrParentInContainer(sliceUid))
            {
                var randVect = rng.NextPolarVector2(2.0f, 2.5f);
                _physics.SetLinearVelocity(sliceUid, randVect);
            }

            // Fills new slice if original entity allows.
            if (solutionInfo.HasValue)
            {
                var (sourceSoln, sourceSolution) = solutionInfo.Value;
                var sliceVolume = sourceSolution.Volume / FixedPoint2.New(slices.Count);

                var lostSolution = _solutionContainer.SplitSolution(sourceSoln, sliceVolume);
                FillSlice(sliceUid, lostSolution);
            }
        }

        _audio.PlayPredicted(ent.Comp.Sound, Transform(ent).Coordinates, user, AudioParams.Default.WithVolume(-2));

        var ev = new BeforeFullySlicedEvent
        {
            User = user
        };
        RaiseLocalEvent(ent, ev);

        return true;
    }

    private bool TryGetVerbDataAndTool(
        EntityUid uid,
        SliceableComponent comp,
        GetVerbsEvent<InteractionVerb> args,
        EntityUid user,
        out bool verbDisabled,
        out string? verbMessage,
        out EntityUid? tool
    )
    {
        tool = null;
        verbMessage = null;
        verbDisabled = false;

        if (args.Using is not null)
        {
            var used = args.Using.Value;
            if (!HasComp<ToolComponent>(used))
                return false;

            if (!_toolSystem.HasQuality(used, comp.ToolQuality))
            {
                verbDisabled = true;
                verbMessage = Loc.GetString("slice-verb-message-tool", ("target", uid));
            }
            tool = used;
        }
        else if (_toolSystem.HasQuality(user, comp.ToolQuality))
        {
            tool = user;
        }
        else
        {
            return false;
        }

        if (TryComp<MobStateComponent>(uid, out var mobState) && !_mob.IsDead(uid, mobState))
        {
            verbDisabled = true;
            verbMessage = Loc.GetString("slice-verb-target-isnt-dead");
        }

        return true;
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

    private void FillSlice(EntityUid sliceUid, Solution solution)
    {
        if (TryComp<EdibleComponent>(sliceUid, out var sliceFoodComp)
            && _solutionContainer.TryGetSolution(sliceUid, sliceFoodComp.Solution, out var itsSoln, out var itsSolution))
        {
            _solutionContainer.RemoveAllSolution(itsSoln.Value);

            var lostSolutionPart = solution.SplitSolution(itsSolution.AvailableVolume);
            _solutionContainer.TryAddSolution(itsSoln.Value, lostSolutionPart);
        }
    }
}

/// <summary>
/// Called for the doafter on the entity that slice something via tool with slicing quality.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class TrySliceDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Called after slicing of the entity.
/// </summary>
[ByRefEvent]
public record struct SliceEvent;
