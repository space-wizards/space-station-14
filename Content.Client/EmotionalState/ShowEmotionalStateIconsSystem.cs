using Content.Shared.Overlays;
using Content.Shared.StatusIcon.Components;
using Content.Shared.EmotionalState;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Client.Overlays;

public sealed class ShowEmotionalStateIconsSystem : EquipmentHudSystem<ShowEmotionalStateIconsComponent>
{
    [Dependency] private readonly EmotionalStateSystem _emotionalState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmotionalStateComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, EmotionalStateComponent component, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;

        if (_emotionalState.TryGetStatusIconPrototype(component, out var iconPrototype))
        { 
            ev.StatusIcons.Add(iconPrototype);
        }

        if (_emotionalState.TryGetDeltaIconPrototype(component, out var deltaIconPrototype))
        {
            ev.StatusIcons.Add(deltaIconPrototype);
        }
    }
}
