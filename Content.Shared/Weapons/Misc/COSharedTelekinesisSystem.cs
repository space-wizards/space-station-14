using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Toggleable;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Misc;

public abstract class COSharedTelekinesisSystem : EntitySystem
{
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

        StartTether(user);
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

    protected virtual void StartTether(EntityUid user)
    { }

    protected virtual void StopTether(EntityUid? telekinesis)
    { }
}
