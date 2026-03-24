cmd-usernameban-help = Usage: usernameban <username> [note]
cmd-usernameban-success = Username '{$username}' banned successfully (ID: {$id})
cmd-usernameban-error = Failed to ban username: {$error}

cmd-usernameunban-help = Usage: usernameunban <ban id>
cmd-usernameunban-invalid-id = Invalid ban ID
cmd-usernameunban-no-admin = Admin user required
cmd-usernameunban-success = Username ban {$id} removed successfully
cmd-usernameunban-error = Failed to remove username ban: {$error}

cmd-usernamewhitelist-help = Usage: usernamewhitelist <username> [note]
cmd-usernamewhitelist-success = Username '{$username}' whitelisted successfully (ID: {$id})
cmd-usernamewhitelist-error = Failed to whitelist username: {$error}

cmd-usernameunwhitelist-help = Usage: usernameunwhitelist <whitelist id>
cmd-usernameunwhitelist-invalid-id = Invalid whitelist ID
cmd-usernameunwhitelist-success = Username whitelist {$id} removed successfully
cmd-usernameunwhitelist-error = Failed to remove username whitelist: {$error}

cmd-usernameregexban-help = Usage: usernameregexban <regex pattern> [note]
cmd-usernameregexban-success = Regex pattern '{$pattern}' banned successfully (ID: {$id})
cmd-usernameregexban-invalid-pattern = Invalid regex pattern: {$error}
cmd-usernameregexban-error = Failed to ban regex pattern: {$error}

cmd-usernameregexunban-help = Usage: usernameregexunban <ban id>
cmd-usernameregexunban-invalid-id = Invalid ban ID
cmd-usernameregexunban-no-admin = Admin user required
cmd-usernameregexunban-success = Regex ban {$id} removed successfully
cmd-usernameregexunban-error = Failed to remove regex ban: {$error}

cmd-usernamerefresh-help = Usage: usernamerefresh - Refreshes the username ban cache from the database
cmd-usernamerefresh-success = Username ban cache refreshed successfully
cmd-usernamerefresh-error = Failed to refresh username ban cache: {$error}
