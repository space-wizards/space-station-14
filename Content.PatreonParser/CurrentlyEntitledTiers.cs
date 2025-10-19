// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace Content.PatreonParser;

public sealed class CurrentlyEntitledTiers
{
    [JsonPropertyName("data")]
    public List<TierData> Data = default!;
}
