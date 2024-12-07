using Content.Shared.Store;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Emag.Components;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Store.Conditions;

/// <summary>
/// Allows a store entry to be filtered out based on the user's access.
/// Supports only AccessReader
/// </summary>
public sealed partial class BuyerAccessCondition : ListingCondition
{
    public override bool Condition(ListingConditionArgs args)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

        var ent = args.EntityManager;
        
        var _accessReader = ent.System<AccessReaderSystem>();

        if (args.StoreEntity == null 
            || !ent.TryGetComponent<AccessReaderComponent>(args.StoreEntity, out var accessReader))
            return true;
        
        if (_accessReader.IsAllowed(args.Buyer, args.StoreEntity.Value, accessReader) 
            || ent.HasComponent<EmaggedComponent>(args.StoreEntity.Value))
            return true;
        
        return false;
    }
}