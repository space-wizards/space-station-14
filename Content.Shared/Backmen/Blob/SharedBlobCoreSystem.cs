using Content.Shared.Alert;
using Content.Shared.Backmen.Blob.Components;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Shared.Backmen.Blob;

public abstract class SharedBlobCoreSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobCoreComponent, DamageChangedEvent>(OnDamaged);
    }

    private static readonly ProtoId<AlertPrototype> BlobHealth = "BlobHealth";

    private void OnDamaged(EntityUid uid, BlobCoreComponent component, DamageChangedEvent args)
    {
        var maxHealth = component.CoreBlobTotalHealth;
        var currentHealth = maxHealth - args.Damageable.TotalDamage;

        if (component.Observer != null)
            _alerts.ShowAlert(component.Observer.Value, BlobHealth, (short) Math.Clamp(Math.Round(currentHealth.Float() / 10f), 0, 20));
    }
}
