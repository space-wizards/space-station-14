using Content.Shared._Starlight.Medical.Limbs;
using Content.Shared.Actions.Components;
using Content.Shared.Starlight.Abstract.Codegen;

namespace Content.Server._Starlight.Medical.Limbs;

[GenerateLocalSubscriptions<IWithAction>]
public sealed partial class CyberLimbSystem : EntitySystem
{
    public void InitializeToggleable()
    {
        SubscribeAllWithAction<LimbAttachedEvent>(IWithActionAttached);
        SubscribeAllWithAction<LimbDetachedEvent>(IWithActionRemoved);
    }

    private void IWithActionRemoved(Entity<IWithAction> _, ref LimbDetachedEvent args) 
        => _actions.RemoveProvidedActions(args.Body, args.Limb);

    private void IWithActionAttached(Entity<IWithAction> _, ref LimbAttachedEvent args) 
        => _actions.GrantContainedActions(_slEnt.Entity<ActionsComponent>(args.Body), _slEnt.Entity<ActionsContainerComponent>(args.Limb));
}
