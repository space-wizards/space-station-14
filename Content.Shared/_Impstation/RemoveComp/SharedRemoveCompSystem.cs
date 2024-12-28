using Content.Shared.Whitelist;
using Content.Shared.IdentityManagement;
using Robust.Shared.Timing;
using System.Diagnostics;

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
        // if the component has RequireAll set to false, log an error and cancel.
        if (!comp.UnwantedComponents.RequireAll)
        {
            Log.Error($"Removecomp on {ToPrettyString(Identity.Entity(uid, EntityManager))} only supports RequireAll = true!");
            return;
        }

        // if there are no components in the list, log an error and cancel.
        if (comp.UnwantedComponents.Components == null)
        {
            Log.Error($"RemoveComp on {ToPrettyString(Identity.Entity(uid, EntityManager))} has no components defined!");
            return;
        }

        // if no errors are detected, run RemoveComponents() and then delete yourself.
        RemoveComponents(uid, comp);
        EntityManager.RemoveComponent<RemoveCompComponent>(uid);
    }

    // This is unfortunately something I have to do because we do not have a team big enough to refactor stuff. Don't be like me.
    /// <summary>
    /// Removes components from the list until there are no components left to remove.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="comp"></param>
    private void RemoveComponents(EntityUid uid, RemoveCompComponent comp)
    {
        // loop through each component in the list,
        foreach (var component in comp.UnwantedComponents.Components!)
        {
            // set a variable to return the type of each component, and...
            var compType = EntityManager.ComponentFactory.GetRegistration(component).Type;

            // set a variable to the return value of EntityManager.RemoveComponent.
            // note to self - methods can be booleans, and will be run in variable declarations. that's damn handy.
            var success = EntityManager.RemoveComponent(uid, compType);

            // assert that var success will return true. if it doesn't, complain about it
            if (!success)
            {
                Log.Error($"RemoveComp on {ToPrettyString(Identity.Entity(uid, EntityManager))} tried to remove component that wasn't present.");
            }
        }
    }
}
