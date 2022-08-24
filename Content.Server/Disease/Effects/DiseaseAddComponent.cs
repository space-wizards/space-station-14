using JetBrains.Annotations;
using Content.Shared.Disease;

namespace Content.Server.Disease.Effects
{
    /// <summary>
    /// Adds a component to the diseased entity
    /// </summary>
    [UsedImplicitly]
    public sealed class DiseaseAddComponent : DiseaseEffect
    {
        /// <summary>
        /// The component that is added at the end of build up
        /// </summary>
        [DataField("comp")]
        public string? Comp = null;

        public override void Effect(DiseaseEffectArgs args)
        {
            if (Comp == null)
                return;

            EntityUid uid = args.DiseasedEntity;
            var newComponent = (Component) IoCManager.Resolve<IComponentFactory>().GetComponent(Comp);
            newComponent.Owner = uid;
            if (!args.EntityManager.HasComponent(uid, newComponent.GetType()))
                args.EntityManager.AddComponent(uid, newComponent);
        }
    }
}
