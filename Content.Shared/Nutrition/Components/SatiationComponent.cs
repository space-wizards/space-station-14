using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// A component which is basically just a collection of <see cref="Satiation"/>s keyed by their
/// <see cref="SatiationTypePrototype"/>s.
/// </summary>
[Access(typeof(SatiationSystem), typeof(PauseSystem))]
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SatiationComponent : Component
{
    [DataField(required: true)]
    [AutoNetworkedField]
    public Dictionary<ProtoId<SatiationTypePrototype>, Satiation> Satiations = [];

    /// <summary>
    /// Checks if this has a <see cref="Satiation"/> of the specified <paramref name="type"/>.
    /// </summary>
    [Access(Other = AccessPermissions.ReadExecute)]
    public bool Has(ProtoId<SatiationTypePrototype> type) => Satiations.ContainsKey(type);

    /// <summary>
    /// This system handles pausing the <see cref="TimeSpan"/> fields in all of the values of <see cref="Satiations"/>.
    /// </summary>
    public sealed class PauseSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<SatiationComponent, EntityUnpausedEvent>(OnEntityUnpaused);
        }

        private static void OnEntityUnpaused(Entity<SatiationComponent> entity, ref EntityUnpausedEvent args)
        {
            foreach (var satiation in entity.Comp.Satiations.Values)
            {
                if (satiation.ProjectedThresholdChangeTime.HasValue)
                {
                    satiation.ProjectedThresholdChangeTime =
                        satiation.ProjectedThresholdChangeTime.Value + args.PausedTime;
                }

                satiation.NextContinuousEffectTime += args.PausedTime;
            }
        }
    }
}
