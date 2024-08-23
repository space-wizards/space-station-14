using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Toggleable;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Misc;

public abstract class COSharedTelekinesisSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] protected readonly SharedHandsSystem _hands = default!;
    [Dependency] protected readonly TagSystem _tagSystem = default!;
    [Dependency] protected readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] protected readonly ActionContainerSystem _actionContainer = default!;

    [ValidatePrototypeId<TagPrototype>]
    protected const string ExtraordTelekinesisTag = "ExtraordTelekinesis";
    [ValidatePrototypeId<EntityPrototype>]
    protected const string AbilityPowerTelekinesis = "COTelekinesisAbilityPower";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<COTelekinesisComponent, MapInitEvent>(OnMapInit);
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

    private void OnMapInit(Entity<COTelekinesisComponent> ent, ref MapInitEvent args)
    {
        _actionContainer.EnsureAction(ent.Owner, ref ent.Comp.Action, ent.Comp.ActionProto);
        Dirty(ent);
    }

    public bool TryTether(Entity<COTelekinesisComponent?> ent, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        EntityUid? telekinesis = CanTether(user);
        if (telekinesis is not null)
        {
            StopTether(telekinesis);
            return false;
        }

        StartTether(ent, user);
        return true;
    }

    protected virtual EntityUid? CanTether(EntityUid user)
    {
        EntityUid? tetherPower = null;

        foreach (var hand in _hands.EnumerateHands(user))
        {
            if (hand.HeldEntity is not null)
            {
                if (_tagSystem.HasTag(hand.HeldEntity.Value, ExtraordTelekinesisTag))
                {
                    tetherPower = hand.HeldEntity;
                    break;
                }
            }
        }

        return tetherPower;
    }

    protected virtual void StartTether(Entity<COTelekinesisComponent?> ent, EntityUid user)
    { }

    protected virtual void StopTether(EntityUid? telekinesis)
    { }
}
