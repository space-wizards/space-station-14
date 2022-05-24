using Content.Shared.Disease;
using JetBrains.Annotations;

namespace Content.Server.Disease.Effects;

[UsedImplicitly]
public sealed class DiseaseAddComp : DiseaseEffect
{
    [DataField("comp")]
    public string? Comp = null;
    public override void Effect(DiseaseEffectArgs args)
    {
        if (Comp == null) return;

        EntityUid uid = args.DiseasedEntity;
        Component newComponent = (Component) IoCManager.Resolve<IComponentFactory>().GetComponent(Comp);
        newComponent.Owner = uid;

        if (!args.EntityManager.HasComponent(uid, newComponent.GetType()))
            args.EntityManager.AddComponent(uid, newComponent);
    }
}
