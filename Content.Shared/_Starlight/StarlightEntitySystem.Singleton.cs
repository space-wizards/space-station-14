using Content.Shared.GameTicking;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Systems;

public sealed partial class StarlightEntitySystem : EntitySystem
{
    private readonly Dictionary<EntProtoId, EntityUid> _entities = [];

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev) => _entities.Clear();

    public bool TryGetSingleton(EntProtoId surgeryOrStep, out EntityUid uid)
    {
        uid = EntityUid.Invalid;

        if (!_prototypes.HasIndex(surgeryOrStep))
        {
            _sawmill.Error("Prototype '{PrototypeId}' is not registered. Cannot retrieve or spawn a singleton entity.", surgeryOrStep); 
            return false;
        }

        if (!_entities.TryGetValue(surgeryOrStep, out uid) || TerminatingOrDeleted(uid))
        {
            uid = Spawn(surgeryOrStep, MapCoordinates.Nullspace);
            _entities[surgeryOrStep] = uid;
        }

        return true;
    }
}
