// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.DeadSpace.Necromorphs.Unitology.Components;

/// <summary>
/// Used for marking regular unitologs as well as storing icon prototypes so you can see fellow unitologs.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedUnitologySystem))]
public sealed partial class UnitologyHeadComponent : Component
{
    [DataField("actionSelectTargetRecruitment", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionSelectTargetRecruitment = "ActionSelectTargetRecruitment";

    [DataField]
    public EntityUid? ActionSelectTargetRecruitmentEntity;

    [DataField("actionUnitologyHead", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionUnitologyHead = "ActionUnitologyHead";

    [DataField]
    public EntityUid? ActionUnitologyHeadEntity;

    [DataField("actionOrderToSlave", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionOrderToSlave = "ActionOrderToSlave";

    [DataField]
    public EntityUid? ActionOrderToSlaveEntity;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float VerbDuration = 10f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int NumberOfCandles = 3;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "UnitologyHeadFaction";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string[] WordsArray = {
        "Дорогой друг, мы собрались здесь сегодня, чтобы объединить наши сердца и умы в поисках истины.",
        "Свет юнитологии освещает наш путь, и мы открываем свои сердца для знаний, которые помогут нам достичь просветления.",
        "Каждый из нас — это уникальная часть великого целого, и вместе мы можем создать гармонию и единство.",
        "Я призываю силы, которые направляют нас, чтобы они помогли нам в этом священном ритуале.",
        "Пусть энергия юнитологии наполнит нас, и пусть наши намерения будут чистыми и искренними.",
        "Мы здесь, чтобы учиться, расти и поддерживать друг друга на нашем пути к свободе и пониманию.",
        "Сейчас, когда мы начинаем этот обряд, давайте сосредоточимся на единстве и любви, которые связывают нас.",
        "Пусть свет юнитологии ведет нас к новым открытиям и внутреннему миру.",
        "Соберем наши силы и откроем двери к новым знаниям, которые ждут нас.",
        "Сейчас мы готовы начать, и пусть этот ритуал станет шагом к нашему общему благу."
    };

    public override bool SessionSpecific => true;
}
