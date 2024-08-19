
# Coloring rule of thumb: green for met requirement, yellow for unmet requirement that can still be met, red for unmeetable

role-timer-department-sufficient = You require [color=green]{TOSTRING($current, "0")}/{TOSTRING($required, "0")}[/color] minutes of [color={$departmentColor}]{$department}[/color] department playtime to play this role.
role-timer-department-insufficient = You require [color=yellow]{TOSTRING($current, "0")}/{TOSTRING($required, "0")}[/color] minutes of [color={$departmentColor}]{$department}[/color] department playtime to play this role.
role-timer-department-not-too-high = You require [color=green]{TOSTRING($current, "0")}/{TOSTRING($required, "0")}[/color] or fewer minutes of [color={$departmentColor}]{$department}[/color] department playtime.
role-timer-department-too-high = You require [color=red]{TOSTRING($current, "0")}/{TOSTRING($required, "0")}[/color] or fewer minutes of [color={$departmentColor}]{$department}[/color] department playtime. (Are you trying to play a trainee role?)

role-timer-overall-sufficient = You require [color=green]{TOSTRING($current, "0")}/{TOSTRING($required, "0")}[/color] minutes of playtime to play this role.
role-timer-overall-insufficient = You require [color=yellow]{TOSTRING($current, "0")}/{TOSTRING($required, "0")}[/color] minutes of playtime to play this role.
role-timer-overall-not-too-high = You require [color=green]{TOSTRING($current, "0")}/{TOSTRING($required, "0")}[/color] or fewer minutes of playtime to play this role.
role-timer-overall-too-high = You require [color=red]{TOSTRING($current, "0")}/{TOSTRING($required, "0")}[/color] or fewer minutes of playtime to play this role. (Are you trying to play a trainee role?)

role-timer-role-sufficient = You require [color=green]{TOSTRING($current, "0")}/{TOSTRING($required, "0")}[/color] minutes of [color={$departmentColor}]{$job}[/color] playtime to play this role.
role-timer-role-insufficient = You require [color=yellow]{TOSTRING($current, "0")}/{TOSTRING($required, "0")}[/color] minutes of [color={$departmentColor}]{$job}[/color] playtime to play this role.
role-timer-role-not-too-high = You require [color=green]{TOSTRING($current, "0")}/{TOSTRING($required, "0")}[/color] or fewer minutes with [color={$departmentColor}]{$job}[/color] to play this role.
role-timer-role-too-high = You require [color=red]{TOSTRING($current, "0")}/{TOSTRING($required, "0")}[/color] or fewer minutes with [color={$departmentColor}]{$job}[/color] to play this role. (Are you trying to play a trainee role?)

role-whitelisted = You [color=green]are[/color] whitelisted to play this role.
role-not-whitelisted = You [color=yellow]are not[/color] whitelisted to play this role.

role-timer-age-old-enough = Your character's age must be at least [color=green]{$age}[/color] to play this role.
role-timer-age-not-old-enough = Your character's age must be at least [color=yellow]{$age}[/color] to play this role.
role-timer-age-young-enough = Your character's age must be at most [color=green]{$age}[/color] to play this role.
role-timer-age-not-young-enough = Your character's age must be at most [color=yellow]{$age}[/color] to play this role.

role-timer-whitelisted-species-pass = Your character [color=green]must[/color] be one of the following species to play this role: [color=green]{$species}[/color]
role-timer-whitelisted-species-fail = Your character [color=yellow]must[/color] be one of the following species to play this role: [color=yellow]{$species}[/color]
role-timer-blacklisted-species-pass = Your character [color=green]must not[/color] be one of the following species to play this role: [color=green]{$species}[/color]
role-timer-blacklisted-species-fail = Your character [color=yellow]must not[/color] be one of the following species to play this role: [color=yellow]{$species}[/color]

role-timer-whitelisted-traits = Your character must have one of the following traits:
role-timer-blacklisted-traits = Your character must not have any of the following traits:

role-timer-locked = Locked (hover for details)

role-timer-department-unknown = Unknown Department

role-ban = You have been banned from this role.
