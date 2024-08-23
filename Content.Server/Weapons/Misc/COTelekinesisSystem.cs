using Content.Shared.Implants;
using Content.Shared.Tag;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Misc;

namespace Content.Server.Weapons.Misc;

public sealed class COTelekinesisSystem : COSharedTelekinesisSystem
{
    [Dependency] protected readonly COSharedTelekinesisHandSystem _telekinesisHand = default!;
    [ValidatePrototypeId<TagPrototype>]
    public const string TelekinesisTag = "ExtraordTelekinesis";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<COTelekinesisComponent, ImplantImplantedEvent>(ImplantGive);
        SubscribeLocalEvent<COTelekinesisComponent, ToggleActionEvent>(TelekinesisAction);
    }

    private void TelekinesisAction(Entity<COTelekinesisComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        TryTelekinesisTether((ent.Owner, ent.Comp), args.Performer);
        args.Handled = true;
    }

    private bool TryTelekinesisTether(Entity<COTelekinesisComponent?> ent, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        bool enabled = TryTether((ent.Owner, ent.Comp), user);

        _actionsSystem.SetToggled(ent.Comp.Action, enabled);
        return enabled;
    }

    /// <summary>
    /// Gives telekinesis powers by moving it from implant to the implanted user
    /// </summary>
    public void ImplantGive(EntityUid uid, COTelekinesisComponent comp, ref ImplantImplantedEvent ev)
    {
        if (ev.Implanted != null)
        {
            EnsureComp<COTelekinesisComponent>(ev.Implanted.Value);
            _tagSystem.TryAddTag(ev.Implanted.Value, TelekinesisTag);
        }
    }

    protected override void StartTether(EntityUid user)
    {
        var coords = Transform(user).Coordinates;

        var entityUid = Spawn(AbilityPowerTelekinesis, coords);

        if (_hands.TryPickup(user, entityUid) == false)
        {
            _popup.PopupEntity(Loc.GetString("telekinesis-no-free-hands"), user);
            EntityManager.DeleteEntity(entityUid);
            return;
        }
    }

    protected override void StopTether(EntityUid? telekinesis)
    {
        if (telekinesis is not null && TryComp<COTelekinesisHandComponent>(telekinesis, out var comp))
        {
            _telekinesisHand.StopTether(telekinesis.Value, comp);
            EntityManager.DeleteEntity(telekinesis);
        }
    }
}
