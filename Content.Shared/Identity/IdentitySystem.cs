using Content.Shared.Ghost;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects.Components.Localization;

namespace Content.Shared.Identity;

public sealed class IdentitySystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly

    private static string SlotName = "identity";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdentityComponent, ComponentInit>(OnInit);
    }

    #region Public API

    /// <summary>
    ///     Returns the entity that should be used for identity purposes, for example to pass into localization.
    /// </summary>
    public EntityUid IdentityEntity(EntityUid uid)
    {
        if (!TryComp<IdentityComponent>(uid, out var identity))
            return uid;

        return identity.IdentityEntitySlot.ContainedEntity ?? uid;
    }

    /// <summary>
    ///     Returns the name that should be used for this entity for identity purposes.
    /// </summary>
    public string IdentityName(EntityUid uid, EntityUid? viewer)
    {
        EntityUid entity = uid;
        if (TryComp<IdentityComponent>(uid, out var identity))
        {
            if (viewer == null || !CanSeeThroughIdentity(uid, viewer.Value))
            {
                entity = identity.IdentityEntitySlot.ContainedEntity ?? uid;
            }
        }

        return Name(entity);
    }

    public bool CanSeeThroughIdentity(EntityUid uid, EntityUid viewer)
    {
        if (uid == viewer)
            return true;

        return HasComp<SharedGhostComponent>(viewer);
    }

    #endregion

    #region Private API & Events

    private void OnInit(EntityUid uid, IdentityComponent component, ComponentInit args)
    {
        component.IdentityEntitySlot = _container.EnsureContainer<ContainerSlot>(uid, SlotName);
        var ident = Spawn(null, Transform(uid).Coordinates);

        // Clone the old entity's grammar to the identity entity, for loc purposes.
        if (TryComp<GrammarComponent>(uid, out var grammar))
        {
            var identityGrammar = EnsureComp<GrammarComponent>(ident);

            foreach (var (k, v) in grammar.Attributes)
            {
                identityGrammar.Attributes.Add(k, v);
            }
        }

        MetaData(ident).EntityName = Name(uid);
        component.IdentityEntitySlot.Insert(ident);
    }

    #endregion
}
