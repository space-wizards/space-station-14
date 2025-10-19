// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.PatreonParser;

public readonly record struct Patron(string FullName, string TierName, DateTime Start);
