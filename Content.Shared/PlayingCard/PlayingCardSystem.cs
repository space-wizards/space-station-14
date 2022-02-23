using Content.Shared.Interaction;
using Content.Shared.PlayingCard;

namespace Content.Shared.PlayingCard.EntitySystems;

public sealed class PlayingCardSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PlayingCardComponent, UseInHandEvent>(OnUseInhand);
    }

    private void OnUseInhand(EntityUid uid, PlayingCardComponent component, UseInHandEvent args)
    {
        if (args.Handled) return;
        FlipCard(component, args.User);
        args.Handled = true;
    }

    private void FlipCard(PlayingCardComponent component, EntityUid user)
    {
        if (component.FacingUp)
        {
            component.FacingUp = false;
            if (TryComp<AppearanceComponent>(component.Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(PlayingCardVisuals.FacingUp, false);
            }
            // use improper name
            // use improper description
        }
        else
        {
            component.FacingUp = true;
            if (TryComp<AppearanceComponent>(component.Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(PlayingCardVisuals.FacingUp, true);
            }
            // assign proper name
            // assign proper description
        }
    }
}
