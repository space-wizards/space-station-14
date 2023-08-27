using Content.Server.Mind;

namespace Content.Server.Roles;

public sealed record RoleAddedEvent(EntityUid MindId, MindComponent Mind, bool Antagonist) : RoleEvent(MindId, Mind, Antagonist);
