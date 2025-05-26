using Content.Server.Administration.Logs;
using Content.Server.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Research.Prototypes;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Xenoborgs;
using Content.Shared.Xenoborgs.Components;

namespace Content.Server.Xenoborgs;

public sealed class XenoborgFactorySystem : SharedXenoborgFactorySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!; //bobby
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    /// <inheritdoc/>
    public override void Reclaim(EntityUid uid, EntityUid item, XenoborgFactoryComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        base.Reclaim(uid, item, component);

        FinishProducing(uid, item);

        var logImpact = HasComp<HumanoidAppearanceComponent>(item) ? LogImpact.Extreme : LogImpact.Medium;
        _adminLogger.Add(LogType.Gib,
            logImpact,
            $"{ToPrettyString(item):victim} was gibbed by {ToPrettyString(uid):entity} ");
        _body.GibBody(item);


        QueueDel(item);
    }

    public void FinishProducing(EntityUid uid, EntityUid item, XenoborgFactoryComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;
        if (!Proto.TryIndex(comp.Recipe, out LatheRecipePrototype? recipe))
            return;
        if (recipe.Result is { } resultProto)
        {
            var result = Spawn(resultProto, Transform(uid).Coordinates);
            BorgChassisComponent? chassis = null;
            EntityUid? brain = null;
            foreach (var (id, _) in _body.GetBodyOrgans(item))
            {
                if (HasComp<BrainComponent>(id))
                {
                    brain = id;
                    break;
                }
            }

            if (brain != null && Resolve(result, ref chassis) && chassis.BrainEntity != null)
            {
                _itemSlots.TryInsert(chassis.BrainEntity.Value, "brain_slot", brain.Value, uid);
            }
        }
    }
}
