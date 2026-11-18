using Content.Shared.Hands.Components;
using Content.Shared.Power.Components;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Power.EntitySystems;

public sealed partial class PowerReceiverSystem : EntitySystem
{
    [Dependency] private EntityQuery<HandsComponent> _handsQuery;
    [Dependency] private SharedPowerReceiverSystem _powerReciver = default!;

    [SubscribeLocalEvent]
    private void AddSwitchPowerVerb(Entity<PowerSwitchComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_handsQuery.HasComp(args.User))
            return;

        var user = args.User;
        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                _powerReciver.TogglePower(ent, user: user);
            },
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png")),
            Text = Loc.GetString("power-switch-component-toggle-verb"),
            Priority = -3
        };
        args.Verbs.Add(verb);

    }
}
