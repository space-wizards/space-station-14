using System.Threading;
using Content.Server.Atmos;
using Content.Shared.Mech.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Mech.Components;

/// <inheritdoc/>
[RegisterComponent, NetworkedComponent]
[ComponentReference(typeof(SharedMechComponent))]
public sealed class MechComponent : SharedMechComponent
{
    [DataField("entryDelay")]
    public float EntryDelay = 3;

    [DataField("exitDelay")]
    public float ExitDelay = 3;

    [DataField("batteryRemovalDelay")]
    public float BatteryRemovalDelay = 2;

    public CancellationTokenSource? EntryTokenSource;

    [DataField("airtight"), ViewVariables(VVAccess.ReadWrite)]
    public bool Airtight = false;

    [DataField("startingEquipment", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> StartingEquipment = new();

    //TODO: this doesn't support a tank implant for
    [ViewVariables(VVAccess.ReadWrite)]
    public GasMixture Air = new (GasMixVolume);
    public const float GasMixVolume = 70f;
}

public sealed class MechEntryFinishedEvent : EntityEventArgs
{
    public EntityUid User;

    public MechEntryFinishedEvent(EntityUid user)
    {
        User = user;
    }
}

public sealed class MechEntryCanclledEvent : EntityEventArgs
{

}

public sealed class MechExitFinishedEvent : EntityEventArgs
{

}

public sealed class MechExitCanclledEvent : EntityEventArgs
{

}

public sealed class MechRemoveBatteryFinishedEvent : EntityEventArgs
{

}

public sealed class MechRemoveBatteryCancelledEvent : EntityEventArgs
{

}
