// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Backmen.Economy;

namespace Content.Server.Backmen.Economy;

[RegisterComponent]
public sealed partial class BankMemoryComponent : Component
{
    public string AccountNumber => BankAccount?.Comp.AccountNumber ?? "";
    public string AccountPin => BankAccount?.Comp.AccountPin ?? "";
    public Entity<BankAccountComponent>? BankAccount { get; set; }
}
