using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Sliceable;

public sealed class SharedSliceableSystem : EntitySystem
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SliceableComponent, TrySliceEvent>(AfterSlicing);
        SubscribeLocalEvent<SliceableComponent, GetVerbsEvent<InteractionVerb>>(AddSliceVerb);
        SubscribeLocalEvent<SliceableComponent, InteractUsingEvent>(OnInteraction);
    }

    private void OnInteraction(EntityUid uid, SliceableComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Tryes to get tool component in held entity, after it on entity itself.
        if (_tool.HasQuality(args.Used, comp.ToolQuality))
        {
            args.Handled = true;
            OnVerbUsing(uid, args.Used, args.User, comp.SliceTime.Seconds, comp.ToolQuality);
            return;
        }
        else if (_tool.HasQuality(args.User, comp.ToolQuality))
        {
            args.Handled = true;
            OnVerbUsing(uid, args.User, args.User, comp.SliceTime.Seconds, comp.ToolQuality);
            return;
        }
    }

    private void AddSliceVerb(EntityUid uid, SliceableComponent comp, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract)
            return;

        var verbDisabled = false;
        var verbMessage = string.Empty;
        EntityUid tool;

        if (args.Using is { } used)
        {
            if (!TryComp<ToolComponent>(used, out var toolComp))
                return;

            if (!_tool.HasQuality(used, comp.ToolQuality))
            {
                verbDisabled = true;
                verbMessage = Loc.GetString("slice-verb-message-tool", ("uid", uid));
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

        if (TryComp<MobStateComponent>(uid, out var mobState) && !_mob.IsDead(uid, mobState))
        {
            verbDisabled = true;
            verbMessage = Loc.GetString("slice-verb-message-alive");
        }

        InteractionVerb verb = new()
        {
            Text = Loc.GetString("slice-verb-name"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/cutlery.svg.192dpi.png")),
            Disabled = verbDisabled,
            Message = verbMessage,
            Act = () =>
            {
                OnVerbUsing(uid, args.User, tool, comp.SliceTime.Seconds, comp.ToolQuality);
            },
        };
        args.Verbs.Add(verb);
    }

    private void OnVerbUsing(EntityUid uid, EntityUid user, EntityUid used, float time, string qualities)
    {
        _tool.UseTool(
            used,
            user,
            uid,
            time,
            qualities,
            new TrySliceEvent());
    }

    private void AfterSlicing(Entity<SliceableComponent> ent, ref TrySliceEvent args)
    {
        var hasBody = TryComp<BodyComponent>(ent, out var body);

        // only show a big popup when butchering living things.
        var popupType = PopupType.Small;
        if (hasBody)
            popupType = PopupType.LargeCaution;

        _popup.PopupEntity(Loc.GetString("slice-butchered-success", ("uid", ent), ("knife", args.Used!)),
            args.User, popupType);

        if (hasBody)
            _body.GibBody(ent, body: body);
        else
            QueueDel(ent);

        TrySlice(ent);
    }

    private bool TrySlice(EntityUid uid,
        SliceableComponent? comp = null,
        FoodComponent? food = null,
        TransformComponent? transform = null)
    {
        if (!Resolve(uid, ref comp, ref transform))
            return false;

        var slices = EntitySpawnCollection.GetSpawns(comp.Slices);

        foreach (var sliceProto in slices)
        {
            var sliceUid = Spawn(sliceProto);

            _transform.DropNextTo(sliceUid, (uid, transform));
            _transform.SetLocalRotation(sliceUid, 0);

            if (!_container.IsEntityOrParentInContainer(sliceUid))
            {
                var randVect = _random.NextVector2(2.0f, 2.5f);
                _physics.SetLinearVelocity(sliceUid, randVect);
            }

            // Fills new slice if comp allows.
            if (Resolve(uid, ref food) && comp.TransferSolution)
            {
                if (!_solutionContainer.TryGetSolution(uid, food.Solution, out var soln, out var solution))
                    return false;

                var sliceVolume = solution.Volume / FixedPoint2.New(slices.Count);

                var lostSolution = _solutionContainer.SplitSolution(soln.Value, sliceVolume);
                FillSlice(sliceUid, lostSolution);
            }
        }

        _audio.PlayPvs(comp.Sound, transform.Coordinates, AudioParams.Default.WithVolume(-2));

        return true;
    }

    private void FillSlice(EntityUid sliceUid, Solution solution)
    {
        if (TryComp<FoodComponent>(sliceUid, out var sliceFoodComp) &&
            _solutionContainer.TryGetSolution(sliceUid, sliceFoodComp.Solution, out var itsSoln, out var itsSolution))
        {
            _solutionContainer.RemoveAllSolution(itsSoln.Value);

            var lostSolutionPart = solution.SplitSolution(itsSolution.AvailableVolume);
            _solutionContainer.TryAddSolution(itsSoln.Value, lostSolutionPart);
        }
    }
}

/// <summary>
/// Called for doafter.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class TrySliceEvent : SimpleDoAfterEvent;
