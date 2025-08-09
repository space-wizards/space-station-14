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
using Content.Shared.Storage;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Sliceable;

public sealed class SliceableSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SliceableComponent, TrySliceDoAfterEvent>(AfterSlicing);
        SubscribeLocalEvent<SliceableComponent, GetVerbsEvent<InteractionVerb>>(AddSliceVerb);
        SubscribeLocalEvent<SliceableComponent, InteractUsingEvent>(OnInteraction);
    }

    private void OnInteraction(EntityUid uid, SliceableComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (_tool.HasQuality(args.Used, comp.ToolQuality))
        {
            CreateDoAfter(uid, args.User, args.Used, comp.SliceTime.Seconds, comp.ToolQuality);
            return;
        }
    }

    private void AddSliceVerb(EntityUid uid, SliceableComponent comp, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var verbText = Loc.GetString("slice-verb-name");
        var verbDisabled = false;
        var verbMessage = Loc.GetString("slice-verb-message-default");
        EntityUid tool;

        if (args.Using is not null)
        {
            var used = args.Using.Value;
            if (!TryComp<ToolComponent>(used, out var toolComp))
                return;

            if (!_tool.HasQuality(used, comp.ToolQuality))
            {
                verbDisabled = true;
                verbMessage = Loc.GetString("slice-verb-message-tool", ("target", uid));
            }
            tool = used;
        }
        else if (_tool.HasQuality(args.User, comp.ToolQuality))
        {
            tool = args.User;
        }
        else
        {
            return;
        }

        if (TryComp<MobStateComponent>(uid, out var mobState))
        {
            verbText = Loc.GetString("slice-verb-name-red");
            if (!_mob.IsDead(uid, mobState))
            {
                verbDisabled = true;
                verbMessage = Loc.GetString("slice-verb-message-alive");
            }
        }

        InteractionVerb verb = new()
        {
            Text = verbText,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/cutlery.svg.192dpi.png")),
            Disabled = verbDisabled,
            Message = verbMessage,
            Act = () =>
            {
                CreateDoAfter(uid, args.User, tool, comp.SliceTime.Seconds, comp.ToolQuality);
            },
        };
        args.Verbs.Add(verb);
    }

    private void CreateDoAfter(EntityUid uid, EntityUid user, EntityUid used, float time, string qualities)
    {
        _tool.UseTool(
            used,
            user,
            uid,
            time,
            qualities,
            new TrySliceDoAfterEvent());
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

        _popup.PopupPredicted(Loc.GetString("slice-butchered-success", ("target", ent.Owner), ("tool", args.Used.Value)),
            Loc.GetString("slice-butchered-success-others", ("user", args.User), ("target", ent.Owner), ("tool", args.Used.Value)),
            args.User, args.User, popupType);

        if (TrySlice(ent.Owner, args.User))
        {
            var ev = new SliceEvent();
            RaiseLocalEvent(ent, ref ev);

            _body.GibBody(ent);
            _destructible.DestroyEntity(ent);
        }
    }

    private bool TrySlice(Entity<TransformComponent?, SliceableComponent?, EdibleComponent?> ent, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return false;

        var slices = EntitySpawnCollection.GetSpawns(ent.Comp2.Slices);

        foreach (var sliceProto in slices)
        {
            var sliceUid = PredictedSpawnNextToOrDrop(sliceProto, ent);
            _transform.SetLocalRotation(sliceUid, 0);

            if (_net.IsServer && slices.Count != 0 && !_container.IsEntityOrParentInContainer(sliceUid))
            {
                var randVect = _random.NextVector2(2.0f, 2.5f);
                _physics.SetLinearVelocity(sliceUid, randVect);
            }

            // Fills new slice if comp allows.
            if (ent.Comp2.TransferSolution && Resolve(ent, ref ent.Comp3))
            {
                if (!_solutionContainer.TryGetSolution(ent.Owner, ent.Comp3.Solution, out var soln, out var solution))
                    return false;

                var sliceVolume = solution.Volume / FixedPoint2.New(slices.Count);

                var lostSolution = _solutionContainer.SplitSolution(soln.Value, sliceVolume);
                FillSlice(sliceUid, lostSolution);
            }
        }

        _audio.PlayPvs(ent.Comp2.Sound, ent.Comp1.Coordinates, AudioParams.Default.WithVolume(-2));

        var ev = new BeforeFullySlicedEvent
        {
            User = user
        };
        RaiseLocalEvent(ent, ev);

        return true;
    }

    private void FillSlice(EntityUid sliceUid, Solution solution)
    {
        if (TryComp<EdibleComponent>(sliceUid, out var sliceFoodComp) &&
            _solutionContainer.TryGetSolution(sliceUid, sliceFoodComp.Solution, out var itsSoln, out var itsSolution))
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
