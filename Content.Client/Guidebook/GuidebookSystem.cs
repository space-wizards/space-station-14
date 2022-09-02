using Content.Shared.Input;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;

namespace Content.Client.Guidebook;

/// <summary>
/// This handles...
/// </summary>
public sealed class GuidebookSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
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
