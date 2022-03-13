using Content.Shared.Disease;
using JetBrains.Annotations;

namespace Content.Server.Disease
{
    [UsedImplicitly]

    /// <summary>
    /// Makes the diseased sneeze or cough
    /// or neither.
    /// </summary>
    public sealed class DiseaseSnough : DiseaseEffect
    {
        [DataField("type")]
        public SneezeCoughType Type = SneezeCoughType.Sneeze;

        public override void Effect(DiseaseEffectArgs args)
        {
            EntitySystem.Get<DiseaseSystem>().SneezeCough(args.DiseasedEntity, args.Disease, Type);
        }
    }
}
