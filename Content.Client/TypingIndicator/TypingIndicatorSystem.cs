using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Content.Shared.TypingIndicator;
using Robust.Shared.Prototypes;

namespace Content.Client.TypingIndicator;

public sealed class TypingIndicatorSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TypingIndicatorComponent, GetStatusIconsEvent>(OnGetStatusIcon);
    }

    private void OnGetStatusIcon(EntityUid uid, TypingIndicatorComponent component, ref GetStatusIconsEvent args)
    {
        if (!_prototype.TryIndex<TypingIndicatorPrototype>(component.Prototype, out var typingIndicatorProto))
            return;

        args.StatusIcons.Add(_prototype.Index<StatusIconPrototype>(typingIndicatorProto.TypingIcon)); // TODO: Get icon by state [None, Idle, Typing]
    }
}
