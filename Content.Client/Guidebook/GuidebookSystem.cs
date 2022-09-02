using System.Linq;
using Content.Client.Verbs;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;

namespace Content.Client.Guidebook;

/// <summary>
/// This handles...
/// </summary>
public sealed class GuidebookSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly VerbSystem _verbSystem = default!;
    private GuidebookWindow _guideWindow = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenGuidebook,
                new PointerInputCmdHandler(HandleOpenGuidebook))
            .Register<GuidebookSystem>();
        _guideWindow = new GuidebookWindow();

        SubscribeLocalEvent<GetGuidesEvent>(OnGetGuidesEvent);
    }

    public void FakeClientActivateInWorld(EntityUid activated)
    {
        var user = _playerManager.LocalPlayer!.ControlledEntity;
        if (user is null)
            return;
        var activateMsg = new ActivateInWorldEvent(user.Value, activated);
        RaiseLocalEvent(activated, activateMsg, true);
    }

    public void FakeClientAltActivateInWorld(EntityUid activated)
    {
        var user = _playerManager.LocalPlayer!.ControlledEntity;
        if (user is null)
            return;
        // Get list of alt-interact verbs
        var verbs = _verbSystem.GetLocalVerbs(activated, user.Value, typeof(AlternativeVerb));

        if (!verbs.Any())
            return;

        _verbSystem.ExecuteVerb(verbs.First(), user.Value, activated);
    }

    public void FakeClientUse(EntityUid activated)
    {
        var user = _playerManager.LocalPlayer!.ControlledEntity;
        if (user is null)
            return;
        var activateMsg = new InteractHandEvent(user.Value, activated);
        RaiseLocalEvent(activated, activateMsg, true);
    }


    private void OnGetGuidesEvent(GetGuidesEvent ev)
    {
        ev.Entries.AddRange(_prototypeManager.EnumeratePrototypes<GuideEntryPrototype>());
    }

    private bool HandleOpenGuidebook(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.State == BoundKeyState.Down)
            _guideWindow.OpenCenteredRight();

        var ev = new GetGuidesEvent();
        RaiseLocalEvent(ev);

        _guideWindow.UpdateGuides(ev.Entries);

        return true;
    }
}

public sealed class GetGuidesEvent : EntityEventArgs
{
    public readonly List<GuideEntry> Entries = new();
}
