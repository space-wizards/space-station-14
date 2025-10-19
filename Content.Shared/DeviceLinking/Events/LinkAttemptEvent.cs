// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Shared.DeviceLinking.Events;

public sealed class LinkAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Source;
    public readonly EntityUid Sink;
    public readonly EntityUid? User;
    public readonly string SourcePort;
    public readonly string SinkPort;

    public LinkAttemptEvent(EntityUid? user, EntityUid source, string sourcePort, EntityUid sink, string sinkPort)
    {
        User = user;
        Source = source;
        SourcePort = sourcePort;
        Sink = sink;
        SinkPort = sinkPort;
    }
}
