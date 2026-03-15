using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Queries;
using Content.Server.NPC.Systems;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Utilises a <see cref="UtilityQueryPrototype"/> to determine the best target and sets it to the Key.
/// </summary>
public sealed partial class UtilityOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField] public string Key = "Target";

    [DataField] public ReturnTypeResult ReturnType = ReturnTypeResult.Highest;

    /// <summary>
    /// The EntityCoordinates of the specified target.
    /// </summary>
    [DataField("keyCoordinates")]
    public string KeyCoordinates = "TargetCoordinates";

    [DataField("proto", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<UtilityQueryPrototype>))]
    public string Prototype = string.Empty;

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var result = _entManager.System<NPCUtilitySystem>().GetEntities(blackboard, Prototype);
        Dictionary<string, object> effects;

        switch (ReturnType)
        {
            case ReturnTypeResult.Highest:
                var target = result.GetHighest();

                if (!target.IsValid())
                {
                    return (false, new Dictionary<string, object>());
                }

                effects = new Dictionary<string, object>()
                {
                    {Key, target},
                    {KeyCoordinates, new EntityCoordinates(target, Vector2.Zero)},
                };

                return (true, effects);

            case ReturnTypeResult.EnumerableDescending:
                var targetList = result.GetEnumerable();

                effects = new Dictionary<string, object>()
                {
                    {"TargetList", targetList},
                };

                return (true, effects);

            default:
                throw new NotImplementedException();
        }
    }

    public enum ReturnTypeResult
    {
        Highest,
        EnumerableDescending
    }
}
