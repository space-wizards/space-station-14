using Content.Server.Polymorph.Systems;
using Content.Shared.Audio;
using Content.Shared.Disease;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Disease.Effects
{
    [UsedImplicitly]
    public sealed class DiseasePolymorph : DiseaseEffect
    {
        [DataField("polymorphId", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<PolymorphPrototype>))]
        [ViewVariables(VVAccess.ReadWrite)]
        public readonly string PolymorphId = default!;

        [DataField("polymorphSound")]
        [ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier? PolymorphSound;

        [DataField("polymorphMessage")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string? PolymorphMessage;

        public override void Effect(DiseaseEffectArgs args)
        {
            EntityUid? polyUid = EntitySystem.Get<PolymorphableSystem>().PolymorphEntity(args.DiseasedEntity, PolymorphId);

            if (PolymorphSound != null && polyUid != null)
                SoundSystem.Play(PolymorphSound.GetSound(), Filter.Pvs(polyUid.Value), polyUid.Value, AudioHelpers.WithVariation(0.2f));

            if (PolymorphMessage != null && polyUid != null)
                EntitySystem.Get<SharedPopupSystem>().PopupEntity(Loc.GetString(PolymorphMessage), polyUid.Value, polyUid.Value, PopupType.Large);
        }
    }
}
