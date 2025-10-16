// SPDX-License-Identifier: MIT

namespace Content.Shared.Beeper;
[ByRefEvent]
public record struct BeepPlayedEvent(bool Muted);

