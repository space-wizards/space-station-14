using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Shared.Destructible;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;

using Robust.Shared.Serialization;

namespace Content.Shared.Sliceable;

public abstract class SharedSliceableSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedToolSystem _tools = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructibleSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SliceableComponent, TrySliceEvent>(AfterSlicing);
        SubscribeLocalEvent<ToolComponent, GetVerbsEvent<InteractionVerb>>(AddSliceVerb);
    }

    private void AfterSlicing(EntityUid uid, SliceableComponent comp, TrySliceEvent args)
    {
        var hasBody = TryComp<BodyComponent>(uid, out var body);

        // only show a big popup when butchering living things.
        var popupType = PopupType.Small;
        if (hasBody)
            popupType = PopupType.LargeCaution;

        _popupSystem.PopupEntity(Loc.GetString("slice-butchered-success", ("target", uid), ("knife", uid)),
            args.User, popupType);

        if (hasBody)
            _bodySystem.GibBody(uid, body: body);
        else
            QueueDel(uid);

        var ev = new SliceEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    private void AddSliceVerb(EntityUid uid, ToolComponent toolComp, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var target = args.Target;

        if (!TryComp<SliceableComponent>(target, out var sliceComp))
            return;

        var verbDisabled = false;
        var verbMessage = string.Empty;

        if (!_tools.HasQuality(uid, sliceComp.ToolQuality))
        {
            verbDisabled = true;
            verbMessage = Loc.GetString("slice-verb-message-tool");
        }

        if (TryComp<MobStateComponent>(args.Target, out var mobState) && !_mobStateSystem.IsDead(args.Target, mobState))
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
                OnVerbUsing(args.Target, args.User, uid, sliceComp.SliceTime, toolComp);
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
