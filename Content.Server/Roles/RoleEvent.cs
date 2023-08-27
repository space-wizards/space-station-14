using Content.Server.Mind;

namespace Content.Server.Roles;

public abstract record RoleEvent(EntityUid MindId, MindComponent Mind, bool Antagonist);
