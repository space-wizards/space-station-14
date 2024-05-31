whitelist-not-whitelisted = You are not whitelisted.

# proper handling for having a min/max or not
whitelist-playercount-invalid = {$min ->
    [0] The whitelist for this server only applies below {$max} players.
    *[other] The whitelist for this server only applies above {$min} {$max ->
        [2147483647] -> players, so you may be able to join later.
       *[other] -> players and below {$max} players, so you may be able to join later.
    }
}
whitelist-not-whitelisted-rp = You are not whitelisted. To become whitelisted, visit our Discord (which can be found at https://spacestation14.io) and check the #rp-whitelist channel.

cmd-whitelistadd-desc = Adds the player with the given username to the server whitelist.
cmd-whitelistadd-help = Usage: whitelistadd <username>
cmd-whitelistadd-existing = {$username} is already on the whitelist!
cmd-whitelistadd-added = {$username} added to the whitelist
cmd-whitelistadd-not-found = Unable to find '{$username}'
cmd-whitelistadd-arg-player = [player]

cmd-whitelistremove-desc = Removes the player with the given username from the server whitelist.
cmd-whitelistremove-help = Usage: whitelistremove <username>
cmd-whitelistremove-existing = {$username} is not on the whitelist!
cmd-whitelistremove-removed = {$username} removed from the whitelist
cmd-whitelistremove-not-found = Unable to find '{$username}'
cmd-whitelistremove-arg-player = [player]

cmd-kicknonwhitelisted-desc = Kicks all non-whitelisted players from the server.
cmd-kicknonwhitelisted-help = Usage: kicknonwhitelisted

ban-banned-permanent = This ban will only be removed via appeal.
ban-banned-permanent-appeal = This ban will only be removed via appeal. You can appeal at {$link}
ban-expires = This ban is for {$duration} minutes and will expire at {$time} UTC.
ban-banned-1 = You, or another user of this computer or connection, are banned from playing here.
ban-banned-2 = The ban reason is: "{$reason}"
ban-banned-3 = Attempts to circumvent this ban such as creating a new account will be logged.

soft-player-cap-full = The server is full!
panic-bunker-account-denied = This server is in panic bunker mode, often enabled as a precaution against raids. New connections by accounts not meeting certain requirements are temporarily not accepted. Try again later
panic-bunker-account-denied-reason = This server is in panic bunker mode, often enabled as a precaution against raids. New connections by accounts not meeting certain requirements are temporarily not accepted. Try again later. Reason: "{$reason}"
panic-bunker-account-reason-account = Your Space Station 14 account is too new. It must be older than {$minutes} minutes
panic-bunker-account-reason-overall = Your overall playtime on the server must be greater than {$hours} hours
