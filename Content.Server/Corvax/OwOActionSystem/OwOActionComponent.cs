using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Corvax.OwOAction;

[RegisterComponent]
public sealed partial class OwOActionComponent : Component
{
    private bool _isON;

    [DataField("actionId", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string OwOAction = "OwOVoice";

    [DataField("action")] // must be a data-field to properly save cooldown when saving game state.
    public EntityUid? OwOActionEntity = null;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsON
    {
        get => _isON;
        set
        {
            if(_isON == value) return;
            _isON = value;
            if(OwOActionEntity != null)
                EntitySystem.Get<SharedActionsSystem>().SetToggled(OwOActionEntity, _isON);

            Dirty();
        }
    }
}
