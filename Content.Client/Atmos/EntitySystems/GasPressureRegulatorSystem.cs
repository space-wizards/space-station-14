using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Atmos.Piping.Binary.Components;

namespace Content.Client.Atmos.EntitySystems;

/// <summary>
/// Represents the client system responsible for managing and updating the gas pressure regulator interface.
/// Inherits from the shared system <see cref="SharedGasPressureRegulatorSystem"/>.
/// </summary>
public sealed partial class GasPressureRegulatorSystem : SharedGasPressureRegulatorSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasPressureRegulatorComponent, AfterAutoHandleStateEvent>(OnValveUpdate);
    }

    private void OnValveUpdate(Entity<GasPressureRegulatorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }

    protected override void UpdateUi(Entity<GasPressureRegulatorComponent> ent)
    {
        if (UserInterfaceSystem.TryGetOpenUi(ent.Owner, GasPressureRegulatorUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }
}
