using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;

namespace Content.Shared.Wavable;

public sealed class WavableSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WavableComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<WavableComponent, GetVerbsEvent<InteractionVerb>>(AddWaveVerb);
        SubscribeLocalEvent<WavableComponent, ExaminedEvent>(OnExamine);
    }

    private void AddWaveVerb(EntityUid uid, WavableComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract)
            return;

        if (!_hands.IsHolding((args.User, args.Hands), uid, out _))
            return;

        InteractionVerb verb = new()
        {
            Text = Loc.GetString("wavable-verb-text"),
            Act = () => Wave(uid, args.User)
        };

        args.Verbs.Add(verb);
    }

    private void OnExamine(Entity<WavableComponent> entity, ref ExaminedEvent args)
    {
        args.PushText(Loc.GetString("wavable-component-examine"));
    }

    private void OnUseInHand(EntityUid uid, WavableComponent component, UseInHandEvent args)
    {
        if(args.Handled)
            return;

        Wave(uid, args.User);

        args.Handled = true;
    }

    /// <summary>
    ///     Waves the given entity, no matter what
    /// </summary>
    public void Wave(EntityUid uid, EntityUid user)
    {
        var selfMessage = Loc.GetString("wavable-component-wave", ("item", uid));
        var othersMessage = Loc.GetString("wavable-component-wave-other", ("user", Identity.Entity(user, EntityManager)), ("item", uid));
        _popup.PopupPredicted(selfMessage, othersMessage, user, user);
    }
}
