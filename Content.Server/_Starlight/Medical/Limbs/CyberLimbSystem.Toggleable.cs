using System.Linq;
using Content.Server.Actions;
using Content.Server.Administration.Systems;
using Content.Shared._Starlight.Medical.Limbs;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Body.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Starlight;
using Content.Shared.Starlight.Medical.Surgery.Effects.Step;
using Content.Shared.Starlight.Medical.Surgery.Events;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.Medical.Limbs;
public sealed partial class CyberLimbSystem : EntitySystem
{
    public void InitializeToggleable()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, LimbAttachedEvent<IWithAction>>(IWithActionAttached);
        SubscribeLocalEvent<BodyComponent, LimbRemovedEvent<IWithAction>>(IWithActionRemoved);
    }

    private void IWithActionRemoved(Entity<BodyComponent> ent, ref LimbRemovedEvent<IWithAction> args) 
        => _actions.RemoveProvidedActions(ent.Owner, args.Limb);

    private void IWithActionAttached(Entity<BodyComponent> ent, ref LimbAttachedEvent<IWithAction> args) 
        => _actions.GrantContainedActions(_slEnt.Entity<ActionsComponent>(ent), _slEnt.Entity<ActionsContainerComponent>(args.Limb));
}
