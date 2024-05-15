using Content.Server.Ninja.Events;
using Content.Server.Objectives.Systems;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Handles the toggle gloves action.
/// </summary>
public sealed class NinjaGlovesSystem : SharedNinjaGlovesSystem
{
    [Dependency] private readonly CodeConditionSystem _codeCondition = default!;
    [Dependency] private readonly SpaceNinjaSystem _ninja = default!;

    protected override void EnableGloves(Entity<NinjaGlovesComponent> ent, Entity<SpaceNinjaComponent> user)
    {
        base.EnableGloves(ent, user);

        // can't use abilities if suit is not equipped, this is checked elsewhere but just making sure to satisfy nullability
        if (user.Comp.Suit is not {} suit)
            return;

        foreach (var ability in ent.Comp.Abilities)
        {
            // prevent doing an objective multiple times by toggling gloves after doing them
            // if it's not tied to an objective always add them anyway
            if (ability.Objective is {} obj && _codeCondition.IsCompleted(user.Owner, obj))
                EntityManager.AddComponents(user, ability.Components);
        }

        // let abilities that use battery power work
        if (_ninja.GetNinjaBattery(user, out var battery, out var _))
        {
            var ev = new NinjaBatteryChangedEvent(battery.Value, suit);
            RaiseLocalEvent(user, ref ev);
        }
    }
}
