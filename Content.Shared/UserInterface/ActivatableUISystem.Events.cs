using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.UserInterface;

public sealed partial class ActivatableUISystem
{
    [SubscribeLocalEvent]
    private void OnStartup(Entity<ActivatableUIComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Key == null)
        {
            Log.Error($"Missing UI Key for entity: {ToPrettyString(ent)}");
            return;
        }

        // TODO BUI
        // set interaction range to zero to avoid constant range checks.
        //
        // if (ent.Comp.InHandsOnly && _uiSystem.TryGetInterfaceData(ent.Owner, ent.Comp.Key, out var data))
        //     data.InteractionRange = 0;
    }

    [SubscribeLocalEvent]
    private void OnActionPerform(Entity<UserInterfaceComponent> ent, ref OpenUiActionEvent args)
    {
        if (args.Handled || args.Key == null)
            return;

        args.Handled = _ui.TryToggleUi(ent.Owner, args.Key, args.Performer);
    }

    [SubscribeLocalEvent]
    private void GetActivationVerb(Entity<ActivatableUIComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (ent.Comp.VerbOnly || !ShouldAddVerb(ent, args))
            return;

        var user = args.User;

        args.Verbs.Add(new ActivationVerb
        {
            Act = () => InteractUI(user, ent),
            Text = Loc.GetString(ent.Comp.VerbText),
            // TODO VERB ICON find a better icon
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
        });
    }

    [SubscribeLocalEvent]
    private void GetVerb(Entity<ActivatableUIComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!ent.Comp.VerbOnly || !ShouldAddVerb(ent, args))
            return;

        var user = args.User;

        args.Verbs.Add(new Verb
        {
            Act = () => InteractUI(user, ent),
            Text = Loc.GetString(ent.Comp.VerbText),
            // TODO VERB ICON find a better icon
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
        });
    }

    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<ActivatableUIComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.VerbOnly)
            return;

        if (ent.Comp.RequiredItems != null)
            return;

        args.Handled = InteractUI(args.User, ent);
    }

    [SubscribeLocalEvent]
    private void OnActivate(Entity<ActivatableUIComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (ent.Comp.VerbOnly)
            return;

        if (ent.Comp.RequiredItems != null)
            return;

        args.Handled = InteractUI(args.User, ent);
    }

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<ActivatableUIComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.VerbOnly)
            return;

        if (ent.Comp.RequiredItems == null)
            return;

        if (_whitelist.IsWhitelistFail(ent.Comp.RequiredItems, args.Used))
            return;

        args.Handled = InteractUI(args.User, ent);
    }

    [SubscribeLocalEvent]
    private void OnUIClose(Entity<ActivatableUIComponent> ent, ref BoundUIClosedEvent args)
    {
        var user = args.Actor;

        if (user != ent.Comp.CurrentSingleUser)
            return;

        if (!Equals(args.UiKey, ent.Comp.Key))
            return;

        SetCurrentSingleUser(ent.AsNullable(), null);
    }

    [SubscribeLocalEvent]
    private void OnBoundInterfaceOpenAttempt(Entity<ActivatableUIComponent> ent, ref BoundUserInterfaceMessageAttempt args)
    {
        if (args.Message is not OpenBoundInterfaceMessage || ent.Comp.Key.Equals(args.UiKey))
            return;

        if (!RaiseCanOpenEventChecks(args.Actor, ent.Owner))
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnHandDeselected(Entity<ActivatableUIComponent> ent, ref HandDeselectedEvent args)
    {
        if (ent.Comp.InHandsOnly && ent.Comp.RequireActiveHand)
            CloseAll(ent.AsNullable());
    }

    [SubscribeLocalEvent]
    private void OnHandUnequipped(Entity<ActivatableUIComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (ent.Comp.InHandsOnly)
            CloseAll(ent.AsNullable());
    }
}
