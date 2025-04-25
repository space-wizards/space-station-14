using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Content.Shared.Interaction;

using Robust.Shared.Utility;

using Robust.Shared.Serialization;

namespace Content.Shared.Sliceable;

public abstract class SharedSliceableSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedToolSystem _tools = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SliceableComponent, TrySliceEvent>(AfterSlicing);
        SubscribeLocalEvent<SliceableComponent, GetVerbsEvent<InteractionVerb>>(AddSliceVerb);
        SubscribeLocalEvent<SliceableComponent, InteractUsingEvent>(OnInteraction);
    }

    private void OnInteraction(EntityUid target, SliceableComponent sliceComp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        ToolComponent? toolComp = null;
        EntityUid? tool = null;

        // Tryes to get tool component in held entity, after it on entity itself.
        if (Resolve(args.Used, ref toolComp))
            tool = args.Used;

        else if (Resolve(args.User, ref toolComp))
            tool = args.User;

        else
            return;

        if (_tools.HasQuality(tool.Value, sliceComp.ToolQuality))
        {
            args.Handled = true;
            OnVerbUsing(target, args.User, tool.Value, sliceComp.SliceTime, toolComp);
        }
    }

    private void AddSliceVerb(EntityUid target, SliceableComponent sliceComp, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var used = args.Using;

        if (!TryComp<ToolComponent>(used, out var toolComp))
            return;

        var verbDisabled = false;
        var verbMessage = string.Empty;

        if (!_tools.HasQuality(used.Value, sliceComp.ToolQuality))
        {
            verbDisabled = true;
            verbMessage = Loc.GetString("slice-verb-message-tool", ("target", target));
        }

        if (TryComp<MobStateComponent>(target, out var mobState) && !_mobStateSystem.IsDead(target, mobState))
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
                OnVerbUsing(target, args.User, used.Value, sliceComp.SliceTime, toolComp);
            },
        };
        args.Verbs.Add(verb);
    }

    private void OnVerbUsing(EntityUid target, EntityUid user, EntityUid used, float time, ToolComponent toolComp)
    {
        _tools.UseTool(
            used,
            user,
            target,
            time,
            toolComp.Qualities,
            new TrySliceEvent());
    }

    private void AfterSlicing(EntityUid uid, SliceableComponent comp, TrySliceEvent args)
    {
        if (args.Used == null)
            return;

        var used = args.Used.Value;
        var hasBody = TryComp<BodyComponent>(uid, out var body);

        // only show a big popup when butchering living things.
        var popupType = PopupType.Small;
        if (hasBody)
            popupType = PopupType.LargeCaution;

        _popupSystem.PopupEntity(Loc.GetString("slice-butchered-success", ("target", uid), ("knife", used)),
            args.User, popupType);

        if (hasBody)
            _bodySystem.GibBody(uid, body: body);
        else
            QueueDel(uid);

        var ev = new SliceEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    ///     Called after doafter.
    /// </summary>
    [ByRefEvent]
    public record struct SliceEvent();
}

/// <summary>
///     Called for doafter.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class TrySliceEvent : SimpleDoAfterEvent;
