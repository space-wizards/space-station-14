using System.Diagnostics.CodeAnalysis;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Storage.EntitySystems;

public sealed partial class EntityProviderSystem
{
    [Dependency] private IPrototypeManager _prototype = default!;

    [PublicAPI]
    public bool TryGetEntity(Entity<EntityProviderComponent?> provider, EntProtoId protoId, [NotNullWhen(true)] out EntityUid? entity)
    {
        entity = null;

        if (!TryGetEntities(provider, protoId, out var entities, 1))
            return false;

        entity = entities[0];

        return true;
    }

    [PublicAPI]
    public bool TryGetEntities(Entity<EntityProviderComponent?> provider, EntProtoId protoId, [NotNullWhen(true)] out List<EntityUid>? entities, int? amount = null)
    {
        entities = [];

        if (!Resolve(provider, ref provider.Comp)
            || !provider.Comp.EntityCounter.TryGetValue(protoId, out var value))
            return false;

        amount = amount == null ? value : Math.Min(amount.Value, value);

        while (amount > 0)
        {
            entities.Add(PredictedSpawnInContainerOrDrop(protoId, provider, ContainerId));
            value--;
            amount--;
        }

        if (value == 0)
            provider.Comp.EntityCounter.Remove(protoId);
        else
            provider.Comp.EntityCounter[protoId] = value;

        Dirty(provider);
        return true;
    }

    [PublicAPI]
    public bool TryGetEntityCounter(Entity<EntityProviderComponent?> provider, [NotNullWhen(true)] out Dictionary<EntProtoId, int>? entityCounter)
    {
        entityCounter = null;
        if (!Resolve(provider, ref provider.Comp))
            return false;

        entityCounter = provider.Comp.EntityCounter;
        return true;
    }

    [PublicAPI]
    public bool TryEjectEntities(Entity<EntityProviderComponent?> provider, EntProtoId protoId, [NotNullWhen(true)] out List<EntityUid>? entities, int? amount = null, EntityUid? user = null)
    {
        entities = [];
        if (!Resolve(provider, ref provider.Comp) || !TryGetEntities(provider, protoId, out entities, amount))
            return false;

        if (entities.Count == 0)
        {
            var message = Loc.GetString("comp-entity-provider-no-ejected");
            _popup.PopupEntity(message, provider, user, PopupType.Medium);
            return false;
        }

        foreach (var entity in entities)
        {
            _container.Remove(entity, provider.Comp.Container);
        }

        if (!_prototype.Resolve(protoId, out var prototype))
            return true;

        var ejectedAmount = amount == null ? "all" : entities.Count.ToString();
        var messageSuccess = Loc.GetString("comp-entity-provider-ejected", ("light", prototype.Name), ("amount", ejectedAmount));
        _popup.PopupEntity(messageSuccess, provider, user, PopupType.Medium);

        return true;
    }
}
