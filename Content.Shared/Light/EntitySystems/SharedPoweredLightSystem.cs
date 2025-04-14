using Content.Server.Light.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Light.Components;

namespace Content.Shared.Light.EntitySystems;

public abstract class SharedPoweredLightSystem : EntitySystem
{
    private void OnInteractUsing(EntityUid uid, Components.PoweredLightComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = InsertBulb(uid, args.Used, component);
    }

    private void OnInteractHand(EntityUid uid, Components.PoweredLightComponent light, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        // check if light has bulb to eject
        var bulbUid = GetBulb(uid, light);
        if (bulbUid == null)
            return;

        var userUid = args.User;
        //removing a broken/burned bulb, so allow instant removal
        if(TryComp<LightBulbComponent>(bulbUid.Value, out var bulb) && bulb.State != LightBulbState.Normal)
        {
            args.Handled = EjectBulb(uid, userUid, light) != null;
            return;
        }

        // removing a working bulb, so require a delay
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, userUid, light.EjectBulbDelay, new PoweredLightDoAfterEvent(), uid, target: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        });

        args.Handled = true;
    }
}
