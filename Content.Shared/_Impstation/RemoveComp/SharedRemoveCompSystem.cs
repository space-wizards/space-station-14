using Content.Shared.Whitelist;
using Content.Shared.IdentityManagement;
using Robust.Shared.Timing;
using Content.Shared.GameTicking;
using System.ComponentModel.DataAnnotations.Schema;

namespace Content.Shared._Impstation.RemoveComp;

public sealed class RemoveCompSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RemoveCompComponent, MapInitEvent>(OnMapInit);
    }


    /// <summary>
    /// complains if an entity has this but it is either defined wrong or undefined, and if not, runs RemoveComponents
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    /// <exception cref="ArgumentException"></exception>
    private void OnMapInit(EntityUid uid, RemoveCompComponent comp, ref MapInitEvent args)
    {
        // log a message and quit if RequireAll is false.
        if (!comp.UnwantedComponents.RequireAll)
        {
            Log.Error("RemoveComp only supports RequireAll = true!");
            return;
        }

        // if there are no components listed, throw an "improperly defined" error.
        if (comp.UnwantedComponents.Components == null)
        {
            Log.Error($"RemoveComp on {ToPrettyString(Identity.Entity(uid, EntityManager))} must use at least 1 component as a filter in UnwantedComponents!");
            throw new ArgumentException($"RemoveComp on {ToPrettyString(Identity.Entity(uid, EntityManager))} must use at least 1 component as a filter in UnwantedComponents!");
        }

        // if the blacklist contains a component that the entity does not possess, throw an error.
        if (_whitelist.IsBlacklistFail(comp.UnwantedComponents, uid))
        {
            Log.Error($"RemoveComp on {ToPrettyString(Identity.Entity(uid, EntityManager))} is trying to remove a component that does not exist.");
            throw new ArgumentException($"RemoveComp on {ToPrettyString(Identity.Entity(uid, EntityManager))} is trying to remove a component that does not exist.");
        }

        // if no errors are detected, run RemoveComponents() and then delete yourself.
        else
        {
            RemoveComponents(uid, comp);
            EntityManager.RemoveComponent<RemoveCompComponent>(uid);
        }
    }

    // This is unfortunately something I have to do because we do not have a team big enough to refactor stuff. Don't be like me.
    /// <summary>
    /// Removes components from the list until there are no components left to remove.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="comp"></param>
    private void RemoveComponents(EntityUid uid, RemoveCompComponent comp)
    {
        // if the entity has any of the components on the list,
        if (_whitelist.IsBlacklistPass(comp.UnwantedComponents, uid))
        {
            // loop through each component in the list,
            for (var i = 0; i < comp.UnwantedComponents.Components!.Length; i++)
            {
                // set a variable to return the type of each component, and...
                var compType = EntityManager.ComponentFactory.GetRegistration(comp.UnwantedComponents.Components[i]).Type;

                // remove the offending components.
                EntityManager.RemoveComponent(uid, compType);
            }
        }
    }
}
