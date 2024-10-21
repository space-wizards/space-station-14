# ban
cmd-ban-desc = Bans somebody
cmd-ban-help = Usage: ban <name or user ID> <reason> [duration in minutes, leave out or 0 for permanent ban]
cmd-ban-player = Unable to find a player with that name.
cmd-ban-invalid-minutes = {$minutes} is not a valid amount of minutes!
cmd-ban-invalid-severity = {$severity} is not a valid severity!
cmd-ban-invalid-arguments = Invalid amount of arguments
cmd-ban-hint = <name/user ID>
cmd-ban-hint-reason = <reason>
cmd-ban-hint-duration = [duration]
cmd-ban-hint-severity = [severity]

cmd-ban-hint-duration-1 = Permanent
cmd-ban-hint-duration-2 = 1 day
cmd-ban-hint-duration-3 = 3 days
cmd-ban-hint-duration-4 = 1 week
cmd-ban-hint-duration-5 = 2 week
cmd-ban-hint-duration-6 = 1 month

# ban panel
cmd-banpanel-desc = Opens the ban panel
cmd-banpanel-help = Usage: banpanel [name or user guid]
cmd-banpanel-server = This can not be used from the server console
cmd-banpanel-player-err = The specified player could not be found

# listbans
cmd-banlist-desc = Lists a user's active bans.
cmd-banlist-help = Usage: banlist <name or user ID>
cmd-banlist-empty = No active bans found for {$user}
cmd-banlistF-hint = <name/user ID>

cmd-ban_exemption_update-desc = Set an exemption to a type of ban on a player.
cmd-ban_exemption_update-help = Usage: ban_exemption_update <player> <flag> [<flag> [...]]
    Specify multiple flags to give a player multiple ban exemption flags.
    To remove all exemptions, run this command and give "None" as only flag.

cmd-ban_exemption_update-nargs = Expected at least 2 arguments
cmd-ban_exemption_update-locate = Unable to locate player '{$player}'.
cmd-ban_exemption_update-invalid-flag = Invalid flag '{$flag}'.
cmd-ban_exemption_update-success = Updated ban exemption flags for '{$player}' ({$uid}).
cmd-ban_exemption_update-arg-player = <player>
cmd-ban_exemption_update-arg-flag = <flag>

cmd-ban_exemption_get-desc = Show ban exemptions for a certain player.
cmd-ban_exemption_get-help = Usage: ban_exemption_get <player>

cmd-ban_exemption_get-nargs = Expected exactly 1 argument
cmd-ban_exemption_get-none = User is not exempt from any bans.
cmd-ban_exemption_get-show = User is exempt from the following ban flags: {$flags}.
cmd-ban_exemption_get-arg-player = <player>

# Ban panel
ban-panel-title = Banning panel
ban-panel-player = Player
ban-panel-ip = IP
ban-panel-hwid = HWID
ban-panel-reason = Reason
ban-panel-last-conn = Use IP and HWID from last connection?
ban-panel-submit = Ban
ban-panel-confirm = Are you sure?
ban-panel-tabs-basic = Basic info
ban-panel-tabs-reason = Reason
ban-panel-tabs-players = Player List
ban-panel-tabs-role = Role ban info
ban-panel-no-data = You must provide either a user, IP or HWID to ban
ban-panel-invalid-ip = The IP address could not be parsed. Please try again
ban-panel-select = Select type
ban-panel-server = Server ban
ban-panel-role = Role ban
ban-panel-minutes = Minutes
ban-panel-hours = Hours
ban-panel-days = Days
ban-panel-weeks = Weeks
ban-panel-months = Months
ban-panel-years = Years
ban-panel-permanent = Permanent
ban-panel-ip-hwid-tooltip = Leave empty and check the checkbox below to use last connection's details
ban-panel-severity = Severity:
ban-panel-erase = Erase chat messages and player from round

# Ban string
server-ban-string = {$admin} created a {$severity} severity server ban that expires {$expires} for [{$name}, {$ip}, {$hwid}], with reason: {$reason}
server-ban-string-no-pii = {$admin} created a {$severity} severity server ban that expires {$expires} for {$name} with reason: {$reason}
server-ban-string-never = never

# Kick on ban
ban-kick-reason = You have been banned

# Username ban
cmd-ban-username-desc = Ban a username and kick any user with that username.
cmd-ban-username-invalid-username = this username is invalid: {$reason}
cmd-ban-username-hint-username = <username>
cmd-ban-username-hint-regex = <regex>
cmd-ban-username-hint-reason = [reason]
cmd-ban-username-hint-upgrade-ban = [(upgrade to ban)true|false]
cmd-ban-username-hint-ban = Users who join with this username will be banned
cmd-ban-username-hint-no-ban = Users who join with this username will be kicked (Default)
cmd-ban-username-regex-help = Usage: banusernameregex <expression> [reason] [extend to ban (true|false)]
cmd-ban-username-help = Usage: banusername <username> [reason] [extend to ban (true|false)]

# Username ban panel
username-ban-panel-pardon = Pardon
username-ban-panel-id = Id:
username-ban-panel-regex = Regex:
username-ban-panel-expression = Expression:
username-ban-panel-ban = Extend to ban:
username-ban-panel-issuer = Issuing admin:
username-ban-panel-time-issued = Issued Time:
username-ban-panel-round = Round Id:
username-ban-panel-retired = Retired:
username-ban-panel-retirer = Retiring admin:
username-ban-panel-tine-retired = Retired Time:
username-ban-panel-message = Message:

cmd-username-whitelist-not-in-db = this username was not present in the database
cmd-whitelist-username-help = Usage: {$command} <username>

cmd-pardonusername-help = Usage: pardonusername <ban id 1> <ban id 2> ...
cmd-pardonusername-send = Sending retire request rule {$id}

# create username ban failed
cmd-ban-username-missing-minimum-permissions = {$admin} ({$adminId}) tried to create a username rule with no ban flag"
cmd-ban-username-missing-minimum-permissions-regex = {$admin} ({$adminId}) tried to create a simple username ban with an invalid username ({$expression})
cmd-ban-username-invalid-simple = {$admin} ({$adminId}) tried to create a simple username ban with an invalid username ({regexRule})
cmd-ban-username-missing-reason = {$admin} ({$adminId}) is creating a username ban without a reason provided for ({$expression})

# kick on username ban
ban-username-kick-message = Your username is not permitted on this server. You may rejoin after changing your username.
ban-username-ban-message = Your username is not permitted on this server. You have been banned.
ban-username-default-reason-simple = An admin has banned your specific username.
ban-username-default-reason-regex = An admin has banned a class of usernames which includes your username.

# username ban panel
username-ban-panel-title = Username Rules
username-ban-panel-submit = Create Banned Username Rule
username-ban-panel-reason = Username Restriction Reason
username-ban-panel-create = Create
username-ban-panel-view = View
username-ban-panel-extend = Extend to ban
username-ban-panel-rule = prohibition rule
username-ban-panel-create-regex = regex
username-ban-panel-refresh = Refresh Username Rules

# create log message
server-username-rule-create = {$admin} created the username rule {$expression} stating: {$message}
