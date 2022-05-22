using Content.Shared.Examine;
using Content.Shared.Verbs;

namespace Content.Shared.Weapons.Ranged;

public abstract partial class SharedNewGunSystem
{
    private void OnExamine(EntityUid uid, NewGunComponent component, ExaminedEvent args)
    {
        var selectColor = component.SelectiveFire == SelectiveFire.Safety ? "lightgreen" : "cyan";
        args.PushMarkup($"Current selected fire mode is [color={selectColor}]{component.SelectiveFire}[/color].");
        args.PushMarkup($"Fire rate is [color=yellow]{component.FireRate}[/color] per second.");
    }

    private void OnAltVerb(EntityUid uid, NewGunComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || component.SelectiveFire == component.AvailableSelectiveFire)
            return;

        foreach (var value in Enum.GetValues<SelectiveFire>())
        {
            if ((component.AvailableSelectiveFire & value) == 0x0) continue;

            AlternativeVerb verb = new()
            {
                Act = () => SelectFire(component, value),
                Text = value.ToString(),
                IconTexture = "/Textures/Interface/VerbIcons/fold.svg.192dpi.png",
                Disabled = component.SelectiveFire == value,
            };

            args.Verbs.Add(verb);
        }
    }

    private void SelectFire(NewGunComponent component, SelectiveFire fire)
    {
        component.SelectiveFire = fire;
        var curTime = Timing.CurTime;
        var cooldown = TimeSpan.FromSeconds(InteractNextFire);

        if (component.NextFire < curTime)
            component.NextFire = curTime + cooldown;
        else
            component.NextFire += cooldown;

        Dirty(component);
    }
}
