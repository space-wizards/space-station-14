### Localization for role ban command

cmd-roleban-desc = Bans a player from a role
cmd-roleban-help = Usage: roleban <name or user ID> <job> <reason> [duration in minutes, leave out or 0 for permanent ban]

## Completion result hints
cmd-roleban-hint-1 = <name or user ID>
cmd-roleban-hint-2 = <job>
cmd-roleban-hint-3 = <reason>
cmd-roleban-hint-4 = [duration in minutes, leave out or 0 for permanent ban]
cmd-roleban-hint-5 = [severity]

cmd-roleban-hint-duration-1 = Permanent
cmd-roleban-hint-duration-2 = 1 day
cmd-roleban-hint-duration-3 = 3 days
cmd-roleban-hint-duration-4 = 1 week
cmd-roleban-hint-duration-5 = 2 week
cmd-roleban-hint-duration-6 = 1 month


### Localization for role unban command

cmd-roleunban-desc = Pardons a player's role ban
cmd-roleunban-help = Usage: roleunban <role ban id>

## Completion result hints
cmd-roleunban-hint-1 = <role ban id>


### Localization for roleban list command

cmd-rolebanlist-desc = Lists the user's role bans
cmd-rolebanlist-help = Usage: <name or user ID> [include unbanned]

## Completion result hints
cmd-rolebanlist-hint-1 = <name or user ID>
cmd-rolebanlist-hint-2 = [include unbanned]


cmd-roleban-minutes-parse = {$time} is not a valid amount of minutes.\n{$help}
cmd-roleban-severity-parse = ${severity} is not a valid severity\n{$help}.
cmd-roleban-arg-count = Invalid amount of arguments.
cmd-roleban-job-parse = Job {$job} does not exist.
cmd-roleban-name-parse = Unable to find a player with that name.
cmd-roleban-existing = {$target} already has a role ban for {$role}.
cmd-roleban-success = Role banned {$target} from {$role} with reason {$reason} {$length}.

cmd-roleban-inf = permanently
cmd-roleban-until =  until {$expires}

# Department bans
cmd-departmentban-desc = Bans a player from the roles comprising a department
cmd-departmentban-help = Usage: departmentban <name or user ID> <department> <reason> [duration in minutes, leave out or 0 for permanent ban]
