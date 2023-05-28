using Content.Server.GameTicking.Rules.Components;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Sandbox;
using Content.Server.StationEvents.Components;
using Content.Shared.Ghost.Roles;
using Content.Shared.Roles;

namespace Content.Server.StationEvents.Events;

public sealed class SecondLifeRule : StationEventSystem<SecondLifeRuleComponent>
{
    [Dependency] private readonly GhostRoleSystem _ghostRole = default!;
    [Dependency] private readonly SandboxSystem _sandbox = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SecondLifeRuleComponent, TakeGhostRoleEvent>(OnTakeGhostRole);
    }

    private void RebuildRoles(EntityUid uid, SecondLifeRuleComponent component)
    {
        bool rolesDirty = false;
        // Add and update roles
        foreach (var roleKvp in component.Roles)
        {
            if (roleKvp.Value != 0)
            {
                // Check we haven't accepted all the entries for this role.
                var numAccepted = component.AcceptedCount.GetValueOrDefault(roleKvp.Key, 0);
                if (numAccepted >= roleKvp.Value)
                {
                    if (component.RoleInfo.TryGetValue(roleKvp.Key, out var ghostInfoDel))
                    {
                        // All the possible slots for this role have been used by players
                        _ghostRole.UnregisterGhostRole(ghostInfoDel);
                        component.RoleInfo.Remove(roleKvp.Key);
                    }

                    // Do not create or update this role, it's exhausted now.
                    continue;
                }
            }

            if (component.RoleInfo.TryGetValue(roleKvp.Key, out var ghostInfo))
            {
                // Already registered this role, just check it for updates.
                if (component.Rules != ghostInfo.Rules || component.Description != ghostInfo.Description)
                {
                    ghostInfo.Rules = component.Rules;
                    ghostInfo.Description = component.Description;

                    // Only need to explicitly call UpdateAllEui if we only update rules and desc without
                    //   adding or removing roles after.
                    rolesDirty = true;
                }
            }
            else
            {
                // This role requires registration. Perhaps the event just started or perhaps someone
                //   added / increased limit on a role with VV

                if (PrototypeManager.TryIndex<JobPrototype>(roleKvp.Key, out var job))
                {
                    var desc = (job.Description != null)
                        ? $"{Loc.GetString(job.Description)} : {Loc.GetString(component.Description)}"
                        : Loc.GetString(component.Description);

                    var info = new GhostRoleInfo
                    {
                        Name = Loc.GetString(job.Name),
                        Description = desc,
                        Rules = Loc.GetString(component.Rules),
                        Owner = uid, // All the Roles point back to the SecondLifeRuleComponent's entity.
                        UserId = roleKvp.Key,
                    };
                    _ghostRole.RegisterGhostRole(ref info);
                    component.RoleInfo[info.UserId] = info;
                }
            }
        }

        List<string>? toRemove = null;
        foreach (var infoKvp in component.RoleInfo)
        {
            // Check this role is still within the Roles dictionary (incase VV deleted it)
            if (!component.Roles.ContainsKey(infoKvp.Key))
            {
                _ghostRole.UnregisterGhostRole(infoKvp.Value);
                if (toRemove == null)
                    toRemove = new();

                toRemove.Add(infoKvp.Key);
            }
        }

        if (toRemove != null)
        {
            // Do the removal outside the component.RuleInfo iteration
            foreach (var rolename in toRemove)
            {
                component.RoleInfo.Remove(rolename);
            }
        }

        if (rolesDirty)
            _ghostRole.UpdateAllEui();
    }

    private void DeregisterAllRoles(EntityUid uid, SecondLifeRuleComponent component)
    {
        foreach (var infoKvp in component.RoleInfo)
        {
            // Check this role is still within the Roles dictionary (incase VV deleted it)
            _ghostRole.UnregisterGhostRole(infoKvp.Value);
        }
        component.RoleInfo.Clear();
    }

    private void OnTakeGhostRole(EntityUid uid, SecondLifeRuleComponent component, ref TakeGhostRoleEvent args)
    {
        args.TookRole = false;
        if (!component.Roles.TryGetValue(args.UserId, out var limit))
            return;

        // The same player cannot spawn twice from a specific SecondLifeRule
        if (component.PlayersSpawned.Contains(args.Player))
        {
            if (!_sandbox.IsSandboxEnabled)
                return;
        }

        if (!GameTicker.SpawnPlayerAgain(args.Player, args.UserId))
            return; // Player was rejected from the role, or something went wrong.

        // Increment the accepted count
        var acceptedCount = 1 + component.AcceptedCount.GetValueOrDefault(args.UserId, 0);
        component.AcceptedCount[args.UserId] = acceptedCount;

        if (limit != 0 && limit <= acceptedCount)
        {
            // This role is now exhausted
            if (component.RoleInfo.TryGetValue(args.UserId, out var ghostInfo))
            {
                _ghostRole.UnregisterGhostRole(ghostInfo);
                component.RoleInfo.Remove(args.UserId);
            }
        }

        // You won't be allowed to spawn again with this specific second life rule.
        component.PlayersSpawned.Add(args.Player);

        args.TookRole = true;
    }

    /// <summary>
    /// Called when the gamerule begins
    /// </summary>
    protected override void Started(EntityUid uid, SecondLifeRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        // This will set up all the ghost roles
        RebuildRoles(uid, component);

        // Announce the event, but only to the dead. We don't want to encourage the living to commit suicide.
        var str = Loc.GetString(component.AnnounceDead, ("announce", Loc.GetString(component.Announce)), ("description", Loc.GetString(component.Description)));
        ChatSystem.SendDeadAnnouncement(str, colorOverride: Color.FromHex("#18abf5"));

    }

    /// <summary>
    /// Called when the gamerule ends
    /// </summary>
    protected override void Ended(EntityUid uid, SecondLifeRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        DeregisterAllRoles(uid, component);

        // Announce it now that it is done (most likely these players have boarded the station by now)
        var str = Loc.GetString(component.Announce);
        ChatSystem.DispatchGlobalAnnouncement(str, colorOverride: Color.FromHex("#18abf5"));
    }
}
