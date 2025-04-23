using Content.Shared.Holiday;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.EntitySelectors;

/// <summary>
/// If the given Holiday is active, Gets spawns from all of the child selectors
/// </summary>
public sealed partial class HolidaySelector : EntityTableSelector
{
    [Dependency] private readonly HolidaySystem _holiday = default!;

    /// <summary>
    /// Holiday prototype ID
    /// </summary>
    [DataField(required: true)]
    public string Holiday = default!;

    [DataField(required: true)]
    public List<EntityTableSelector> Children = default!;

    protected override IEnumerable<EntProtoId> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto)
    {
        if (_holiday.IsCurrentlyHoliday(Holiday))
        {
            foreach (var child in Children)
            {
                foreach (var spawn in child.GetSpawns(rand, entMan, proto))
                {
                    yield return spawn;
                }
            }
        }
        yield break;
    }
}
