namespace Content.Shared.Procedural.Rewards;

/// <summary>
/// Payout to the station's bank account.
/// </summary>
public sealed class BankReward : ISalvageReward
{
    [DataField("amount")]
    public int Amount = 0;
}
