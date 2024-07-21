using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling;

[RegisterComponent, NetworkedComponent]
public abstract partial class ChangelingActionComponent : Component
{
    [DataField] public float ChemicalCost = 0;

    /// <summary>
    ///     If a changeling can use the action while being in lesser form (a monkey)
    /// </summary>
    [DataField] public bool UseWhileLesserForm = false;

    /// <summary>
    ///     How many humanoids does a changeling have to absorb to unlock the ability
    /// </summary>
    [DataField] public float RequireAbsorbed = 0;

    /// <summary>
    ///     If a squelch is to be played
    /// </summary>
    [DataField] public bool Audible = false;

    [DataField] public BaseActionEvent? Event = null;
}

[RegisterComponent, NetworkedComponent]
public abstract partial class ChangelingActionStingComponent : ChangelingActionComponent
{
    /// <summary>
    ///     Indicates if a changeling must use the sting on himself
    /// </summary>
    [DataField] public bool UseOnSelf = false;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingActionReagentStingComponent : ChangelingActionStingComponent
{
    [DataField] public Dictionary<EntProtoId, FixedPoint2>? Reagents;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingActionEquipComponent : ChangelingActionComponent
{
    [DataField] public List<EntProtoId> Entities;
}

public abstract partial class ChangelingActionEvent : InstantActionEvent { }
public abstract partial class ChangelingStingEvent : EntityTargetActionEvent { }
public abstract partial class ChangelingReagentStingEvent : EntityTargetActionEvent { }
public abstract partial class ChangelingEquipEvent : InstantActionEvent { }

#region Events - Basic

public sealed partial class OpenEvolutionMenuEvent : InstantActionEvent { }
public sealed partial class AbsorbDNAEvent : EntityTargetActionEvent { }
public sealed partial class StingExtractDNAEvent : EntityTargetActionEvent { }
public sealed partial class ChangelingTransformCycleEvent : InstantActionEvent { }
public sealed partial class ChangelingTransformEvent : InstantActionEvent { }
public sealed partial class EnterStasisEvent : InstantActionEvent { }
public sealed partial class ExitStasisEvent : InstantActionEvent { }

#endregion

#region Events - Combat

public sealed partial class ToggleArmbladeEvent : InstantActionEvent { }
public sealed partial class CreateBoneShardEvent : InstantActionEvent { }
public sealed partial class ToggleChitinousArmorEvent : InstantActionEvent { }
public sealed partial class ToggleOrganicShieldEvent : InstantActionEvent { }
public sealed partial class ShriekDissonantEvent : InstantActionEvent { }
public sealed partial class ShriekResonantEvent : InstantActionEvent { }
public sealed partial class ToggleStrainedMusclesEvent : InstantActionEvent { }

#endregion

#region Events - Sting

public sealed partial class StingBlindEvent : EntityTargetActionEvent { }
public sealed partial class StingCryoEvent : EntityTargetActionEvent { }
public sealed partial class StingLethargicEvent : EntityTargetActionEvent { }
public sealed partial class StingMuteEvent : EntityTargetActionEvent { }
public sealed partial class StingFakeArmbladeEvent : EntityTargetActionEvent { }
public sealed partial class StingTransformEvent : EntityTargetActionEvent { }

#endregion

#region Events - Utility

public sealed partial class ActionAnatomicPanaceaEvent : InstantActionEvent { }
public sealed partial class ActionAugmentedEyesightEvent : InstantActionEvent { }
public sealed partial class ActionBiodegradeEvent : InstantActionEvent { }
public sealed partial class ActionChameleonSkinEvent : InstantActionEvent { }
public sealed partial class ActionEphedrineOverdoseEvent : InstantActionEvent { }
public sealed partial class ActionFleshmendEvent : InstantActionEvent { }
public sealed partial class ActionLastResortEvent : InstantActionEvent { }
public sealed partial class ActionLesserFormEvent : InstantActionEvent { }
public sealed partial class ActionSpacesuitEvent : InstantActionEvent { }
public sealed partial class ActionHivemindAccessEvent : InstantActionEvent { }
public sealed partial class ActionContortBodyEvent : InstantActionEvent { }

#endregion
