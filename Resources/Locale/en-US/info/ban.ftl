# ban
cmd-ban-desc = Bans somebody
cmd-ban-help = Usage: ban <name or user ID> <reason> [duration in minutes, leave out or 0 for permanent ban]
cmd-ban-player = Unable to find a player with that name.
cmd-ban-self = You can't ban yourself!
cmd-ban-hint = <name/user ID>
cmd-ban-hint-reason = <reason>
cmd-ban-hint-duration = [duration]

cmd-ban-hint-duration-1 = Permanent
cmd-ban-hint-duration-2 = 1 day
cmd-ban-hint-duration-3 = 3 days
cmd-ban-hint-duration-4 = 1 week
cmd-ban-hint-duration-5 = 2 week
cmd-ban-hint-duration-6 = 1 month

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
