using System.Linq;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Starlight.Restrict;
public abstract partial class SharedRestrictSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<RestrictByUserTagComponent, AttemptMeleeEvent>(OnAttemptMelee);
        SubscribeLocalEvent<RestrictByUserTagComponent, InteractionAttemptEvent>(OnAttemptInteract);
    }

    private void OnAttemptInteract(Entity<RestrictByUserTagComponent> ent, ref InteractionAttemptEvent args)
    {
        if (!_tagSystem.HasAllTags(args.Uid, ent.Comp.Contains) || _tagSystem.HasAnyTag(args.Uid, ent.Comp.DoestContain))
        {
            args.Cancelled = true;
            if (ent.Comp.Messages.Count != 0)
                _popup.PopupClient(Loc.GetString(_random.Pick(ent.Comp.Messages)), args.Uid);
        }
    }

    private void OnAttemptMelee(Entity<RestrictByUserTagComponent> ent, ref AttemptMeleeEvent args)
    {
        if(!_tagSystem.HasAllTags(args.User, ent.Comp.Contains) || _tagSystem.HasAnyTag(args.User, ent.Comp.DoestContain))
        {
            args.Cancelled = true;
            if(ent.Comp.Messages.Count != 0)
                args.Message = Loc.GetString(_random.Pick(ent.Comp.Messages));
        }
    }
}