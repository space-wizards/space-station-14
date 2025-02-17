using Content.Shared.DeadSpace.Items.Cards.Components;
using Content.Shared.DeadSpace.Items.Cards;
using Robust.Client.GameObjects;

namespace Content.Client.DeadSpace.Items.Cards;

public sealed class CardSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CardComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, CardComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData<bool>(uid, CardVisuals.Reserve, out var reserveCard, args.Component))
        {
            if (reserveCard)
                args.Sprite.LayerSetState(0, component.ReserveState);
            else
                args.Sprite.LayerSetState(0, component.State);
        }
    }
}
