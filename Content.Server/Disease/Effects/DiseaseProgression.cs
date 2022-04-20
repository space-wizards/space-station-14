using Content.Server.Disease.Components;
using JetBrains.Annotations;
using Content.Shared.Disease;
using Robust.Shared.IoC;

namespace Content.Server.Disease.Effects
{
    /// <summary>
    /// Handles a disease which incubates over a period of time
    /// before adding another component to the infected entity
    /// currently used for zombie virus
    /// </summary>
    [UsedImplicitly]
    public sealed class DiseaseProgression : DiseaseEffect
    {
        /// <summary>
        /// The rate that's increased over time. Defaults to 1% so the probability can be varied in yaml
        /// </summary>
        [DataField("rate")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Rate = 0.01f;

        /// <summary>
        /// The component that is added at the end of build up
        /// </summary>
        [DataField("comp")]
        public string? Comp = null;

        public override void Effect(DiseaseEffectArgs args)
        {
            args.EntityManager.EnsureComponent<DiseaseBuildupComponent>(args.DiseasedEntity, out var buildup);
            if (buildup.Progression < 1) //increases steadily until 100%
            {
                buildup.Progression += Rate;
            }
            else if (Comp != null)//adds the component for the later stage of the disease.
            {
                EntityUid uid = args.DiseasedEntity;
                var newComponent = (Component) IoCManager.Resolve<IComponentFactory>().GetComponent(Comp);
                newComponent.Owner = uid;
                if (!args.EntityManager.HasComponent(uid, newComponent.GetType()))
                    args.EntityManager.AddComponent(uid, newComponent);
            }
        }
    }
}
