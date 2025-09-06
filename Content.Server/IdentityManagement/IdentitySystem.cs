using Content.Server.CriminalRecords.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;

namespace Content.Server.IdentityManagement;

/// <summary>
///     Responsible for updating the identity of an entity on init or clothing equip/unequip.
/// </summary>
public sealed class IdentitySystem : SharedIdentitySystem
{
    [Dependency] private readonly CriminalRecordsConsoleSystem _criminalRecordsConsole = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdentityComponent, IdentityChangedEvent>(SetIdentityCriminalIcon);
    }

    /// <summary>
    ///     When the identity of a person is changed, searches the criminal records to see if the name of the new identity
    ///     has a record. If the new name has a criminal status attached to it, the person will get the criminal status
    ///     until they change identity again.
    /// </summary>
    private void SetIdentityCriminalIcon(Entity<IdentityComponent> ent, ref IdentityChangedEvent _)
    {
        _criminalRecordsConsole.CheckNewIdentity(ent);
    }
}
