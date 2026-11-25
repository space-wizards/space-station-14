namespace Content.PatreonParser;

public readonly record struct Patron(string FullName, string TierName, DateTime Start);
