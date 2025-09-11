using Content.Server.Ninja.Events;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Handles the toggle gloves action.
/// </summary>
public sealed class NinjaGlovesSystem : SharedNinjaGlovesSystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly SpaceNinjaSystem _ninja = default!;

    protected override void EnableGloves(Entity<NinjaGlovesComponent> ent, Entity<SpaceNinjaComponent> user)
    {
        base.EnableGloves(ent, user);

        // can't use abilities if suit is not equipped, this is checked elsewhere but just making sure to satisfy nullability
        if (user.Comp.Suit is not {} suit)
            return;

        if (!_mind.TryGetMind(user, out var mindId, out var mind))
            return;

        foreach (var ability in ent.Comp.Abilities)
        {
            // non-objective abilities are added in shared already
            if (ability.Objective is not {} objId)
                continue;

            // prevent doing an objective multiple times by toggling gloves after doing them
            // if it's not tied to an objective always add them anyway
            if (!_mind.TryFindObjective((mindId, mind), objId, out var obj))
            {
                Log.Error($"Ninja glove ability of {ent} referenced missing objective {ability.Objective} of {_mind.MindOwnerLoggingString(mind)}");
                continue;
            }

            if (!_objectives.IsCompleted(obj.Value, (mindId, mind)))
                EntityManager.AddComponents(user, ability.Components);
        }

        // let abilities that use battery power work
        if (_ninja.GetNinjaBattery(user, out var battery, out var _))
        {
            var ev = new NinjaBatteryChangedEvent(battery.Value, suit);
            RaiseLocalEvent(user, ref ev);
            RaiseLocalEvent(suit, ref ev);
        }
    }
}
