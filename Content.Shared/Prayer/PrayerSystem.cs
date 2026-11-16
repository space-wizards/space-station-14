using Content.Shared.Bible.Components;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Shared.Prayer;

/// <summary>
/// System to handle subtle messages and praying.
/// </summary>
/// <remarks>
/// Rain is a professional developer and this did not take 2 PRs to fix subtle messages.
/// </remarks>
public abstract partial class PrayerSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void AddPrayVerb(Entity<PrayableComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        var user = args.User;

        // if the user doesn't have an actor then don't add the verb
        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        // this is to prevent ghosts from using it
        if (!args.CanInteract)
            return;

        var prayerVerb = new ActivationVerb
        {
            Text = Loc.GetString(ent.Comp.Verb),
            Icon = ent.Comp.VerbImage,
            Act = () =>
            {
                if (ent.Comp.BibleUserOnly && !HasComp<BibleUserComponent>(user))
                {
                    _popup.PopupEntity(Loc.GetString("prayer-popup-notify-pray-locked"),
                        ent.Owner,
                        actor.PlayerSession,
                        PopupType.Large);
                    return;
                }

                OpenPrayerDialog(ent, user, actor.PlayerSession);
            },
            Impact = LogImpact.Low,
        };
        args.Verbs.Add(prayerVerb);
    }

    /// <summary>
    /// Sends a message to the admin channel with a message and username.
    /// </summary>
    /// <param name="ent">Prayable entity used to make the prayer.</param>
    /// <param name="sender">The IPlayerSession who sent the original message.</param>
    /// <param name="message">Message to be sent to the admin chat.</param>
    /// <remarks>
    /// You may be wondering, "Why the admin chat, specifically? Nobody even reads it!"
    /// Exactly.
    /// </remarks>
    public virtual void Pray(Entity<PrayableComponent> ent, ICommonSession sender, string message) { }

    protected virtual void OpenPrayerDialog(Entity<PrayableComponent> ent, EntityUid user, ICommonSession session) { }
}
