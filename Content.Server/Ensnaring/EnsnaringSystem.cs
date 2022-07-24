using Content.Server.Ensnaring.Components;
using Content.Shared.Alert;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Interaction;

namespace Content.Server.Ensnaring;

public sealed class EnsnaringSystem : EntitySystem
{
    //TODO: AfterInteractionEvent is for testing purposes only, needs to be reworked into a rightclick verb
    [Dependency] private readonly EnsnareableSystem _ensnareable = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnsnaringComponent, AfterInteractEvent>(OnAfterInteraction);
    }

    private void OnAfterInteraction(EntityUid uid, EnsnaringComponent component, AfterInteractEvent args)
    {
        //TODO: This small bit works and works with speed.
        //Obviously once all the major logic is out of the way this needs to be nuked in favor of the steptrigger and throw
        if (args.Target != null)
        {
            TryEnsnare(uid, args.Target.Value, component);
        }
    }

    /// <summary>
    /// Used where you want to try to ensnare an entity with the <see cref="EnsnareableComponent"/>
    /// </summary>
    /// <param name="ensnaringEntity">The entity that will be used to ensnare</param>
    /// <param name="target">The entity that will be ensnared</param>
    /// <param name="component">The ensnaring component</param>
    public void TryEnsnare(EntityUid ensnaringEntity, EntityUid target, EnsnaringComponent component)
    {
        //Don't do anything if they don't have the ensnareable component.
        if (!TryComp<EnsnareableComponent>(target, out var ensnareable))
            return;

        ensnareable.EnsnaringEntity = ensnaringEntity;
        ensnareable.Container.Insert(ensnaringEntity);
        ensnareable.IsEnsnared = true;

        _ensnareable.UpdateAlert(ensnareable);
        var ev = new EnsnareChangeEvent(ensnaringEntity, component.WalkSpeed, component.SprintSpeed);
        RaiseLocalEvent(target, ev, false);
    }
}
