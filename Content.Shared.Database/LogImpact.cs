namespace Content.Shared.Database;

// DO NOT CHANGE THE NUMERIC VALUES OF THESE
[Serializable]
public enum LogImpact : sbyte
{
    Low = -1, // General logging
    Medium = 0, // Has impact on the round but not necessary for admins to be notified of
    High = 1, // Notable logs that come up in normal gameplay; new players causing these will pop up as admin alerts!
    Extreme = 2 // Irreversible round-impacting logs admins should always be notified of, OR big admin actions!!
}
