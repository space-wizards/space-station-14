// SPDX-License-Identifier: MIT

namespace Content.PatreonParser;

public readonly record struct Patron(string FullName, string TierName, DateTime Start);
