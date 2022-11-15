using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;

namespace Content.Shared.Corvax.Sponsors;

[Virtual]
public class SponsorsManager
{
    [Dependency] private readonly MarkingManager _markingMgr = default!;
    
    public void FilterSponsorMarkings(bool isSponsor, ICharacterProfile profile)
    {
        if (profile is not HumanoidCharacterProfile humanoid)
            return;
        
        var toRemove = new List<Marking>();
        foreach (var marking in humanoid.Appearance.Markings)
        {
            if (!_markingMgr.TryGetMarking(marking, out var prototype))
            {
                toRemove.Add(marking);
                continue;
            }

            if (prototype.SponsorOnly && !isSponsor)
            {
                toRemove.Add(marking);
            }
        }

        foreach (var marking in toRemove)
        {
            humanoid.Appearance.Markings.Remove(marking);
        }
    }
}