using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Flesh;

[Serializable, NetSerializable]
public sealed class FleshCultistDevourDoAfterEvent : SimpleDoAfterEvent
{

}

[Serializable, NetSerializable]
public sealed class FleshCultistInfectionDoAfterEvent : SimpleDoAfterEvent
{

}

[Serializable, NetSerializable]
public sealed class FleshCultistInsulatedImmunityMutationEvent : SimpleDoAfterEvent
{

}

[Serializable, NetSerializable]
public sealed class FleshCultistPressureImmunityMutationEvent : SimpleDoAfterEvent
{

}

[Serializable, NetSerializable]
public sealed class FleshCultistFlashImmunityMutationEvent : SimpleDoAfterEvent
{

}

[Serializable, NetSerializable]
public sealed class FleshCultistColdTempImmunityMutationEvent : SimpleDoAfterEvent
{

}

public sealed class FleshCultistAcidSpitActionEvent : WorldTargetActionEvent
{

}

public sealed class FleshCultistShopActionEvent : InstantActionEvent
{

}

public sealed class FleshCultistBladeActionEvent : InstantActionEvent
{

}


public sealed class FleshCultistClawActionEvent : InstantActionEvent
{

}

public sealed class FleshCultistFistActionEvent : InstantActionEvent
{

}

public sealed class FleshCultistSpikeHandGunActionEvent : InstantActionEvent
{

}

public sealed class FleshCultistArmorActionEvent : InstantActionEvent
{

}

public sealed class FleshCultistSpiderLegsActionEvent : InstantActionEvent
{

}

public sealed class FleshCultistBreakCuffsActionEvent : InstantActionEvent
{

}

public sealed class FleshCultistAdrenalinActionEvent : InstantActionEvent
{

}

public sealed class FleshCultistCreateFleshHeartActionEvent : InstantActionEvent
{

}

public sealed class FleshCultistThrowWormActionEvent : InstantActionEvent
{

}

public sealed class FleshCultistAbsorbBloodPoolActionEvent : InstantActionEvent
{

}

public sealed class FleshCultistDevourActionEvent : EntityTargetActionEvent
{

}

public sealed class FleshCultistInfectionActionEvent : EntityTargetActionEvent
{

}



