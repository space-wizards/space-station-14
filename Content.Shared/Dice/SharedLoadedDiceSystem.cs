using Content.Shared.Hands;

namespace Content.Shared.Dice;

public abstract class SharedLoadedDiceSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LoadedDiceComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        SubscribeLocalEvent<LoadedDiceComponent, GotUnequippedHandEvent>(OnGotUnequippedHand);
    }

    private void OnGotEquippedHand(EntityUid uid, LoadedDiceComponent component, EquippedHandEvent args)
    {
        component.User = args.User;
    }

    private void OnGotUnequippedHand(EntityUid uid, LoadedDiceComponent component, UnequippedHandEvent args)
    {
        component.User = null;
    }
}
