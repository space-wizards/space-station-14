role-timer-department-insufficient = Require {$time} more minutes in {$department} department.
role-timer-overall-insufficient = Require {$time} more minutes of playtime.
role-timer-role-insufficient = Require {$time} more minutes with {$job} for this role.

role-timer-locked = Locked (hover for details)

parse-minutes-fail = Unable to parse '{$minutes}' as minutes
parse-session-fail = Did not find session for '{$username}'

## Role Timer Commands

# - AddOverallTime
cmd-addoveralltime-desc = Adds the specified minutes to a player's overall playtime
cmd-addoveralltime-help = Usage: {$command} <user name> <minutes>
cmd-addoveralltime-succeed = Increased overall time for {$username} to {TOSTRING($time, "0")}
cmd-addoveralltime-arg-user = <user name>
cmd-addoveralltime-arg-minutes = <minutes>
cmd-addoveralltime-error-args = Expected exactly two arguments

# - AddRoleTime
cmd-addroletime-desc = Adds the specified minutes to a player's role playtime
cmd-addroletime-help = Usage: {$command} <user name> <role> <minutes>
cmd-addroletime-succeed = Increased role playtime for {$username} / \'{$role}\' to {TOSTRING($time, "0")}
cmd-addroletime-arg-user = <user name>
cmd-addroletime-arg-role = <role>
cmd-addroletime-arg-minutes = <minutes>
cmd-addroletime-error-args = Expected exactly three arguments

# - GetOverallTime
cmd-getoveralltime-desc = Gets the specified minutes for a player's overall playtime
cmd-getoveralltime-help = Usage: {$command} <user name>
cmd-getoveralltime-success = Overall time for {$username} is {TOSTRING($time, "0")} minutes
cmd-getoveralltime-arg-user = <user name>
cmd-getoveralltime-error-args = Expected exactly one argument

# - GetRoleTimer
cmd-getroletimers-desc = Gets all or one role timers from a player
cmd-getroletimers-help = Usage: {$command} <user name> [role]
cmd-getroletimers-no = Found no role timers
cmd-getroletimers-role = Role: {$role}, Playtime: {$time}
cmd-getroletimers-overall = Overall playtime is {$time}
cmd-getroletimers-succeed = Playtime for {$username} is: {TOSTRING($time, "0")}
cmd-getroletimers-arg-user = <user name>
cmd-getroletimers-arg-role = <role|'Overall'>
cmd-getroletimers-error-args = Expected exactly one or two arguments

# - SaveTime
cmd-savetime-desc = Saves the player's playtimes to the db
cmd-savetime-help = Usage: {$command} <user name>
cmd-savetime-succeed = Saved playtime for {$username}
cmd-savetime-arg-user = <user name>
cmd-savetime-error-args = Expected exactly one argument

