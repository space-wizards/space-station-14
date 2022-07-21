role-timer-department-insufficient = Require {$time} more minutes in {$department} department.
role-timer-overall-insufficient = Require {$time} more minutes of playtime.
role-timer-role-insufficient = Require {$time} more minutes with {$job} for this role.

role-timer-locked = Locked (hover for details)

parse-minutes-fail = Unable to parse {$minutes} as minutes
parse-session-fail = Did not find session for {$userid}
parse-userid-fail = Did not find userid for {$userid}

# Commands
# - AddOverallTime
add-overall-time-desc = Adds the specified minutes to a player's overall playtime
add-overall-time-help = Usage: {$command} <netuserid> <minutes>
add-overall-time-help-plain = Name a player to get the role timer information from
add-overall-time-succeed = Increased overall time for {$username} to {$time}

# - AddRoleTime
add-role-time-desc = Adds the specified minutes to a player's role playtime
add-role-time-help = Usage: {$command} <netuserid> <role> <minutes>
add-role-time-help-plain = Name a player to get the role timer information from
add-role-time-succeed = Increased role playtime for {$userid} / \'{$role}\' to {$time}

# - GetOverallTime
get-overall-time-desc = Gets the specified minutes for a player's overall playtime
get-overall-time-help = Usage: {$command} <netuserid>
get-overall-time-help-plain = Name a player to get the role timer information from
get-overall-time-success = Overall time for {$userid} is {$time} minutes

# - GetRoleTimer
get-role-time-desc = Gets all or one role timers from a player
get-role-time-help = Usage: {$command} <name or user ID> [role]
get-role-time-help-plain = Name a player to get the role timer information from
get-role-time-no = Found no role timers
get-role-time-role = Role: {$role}, Playtime: {$time}
get-role-time-overall = Overall playtime is {$time}
get-role-time-succeed = Playtime for {$userid} is: {$time}

# - SaveTime
save-time-desc = Saves the player's playtimes to the db
save-time-help = Usage: {$command} <netuserid>
save-time-help-plain = Name a player to get the role timer information from
save-time-succeed = Saved playtime for {$userid}

