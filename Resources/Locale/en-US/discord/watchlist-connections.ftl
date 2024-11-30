discord-watchlist-connection-header =
    { $players ->
        [one] {$players} player on a watchlist has
        *[other] {$players} players on a watchlist have
    } connected to {$serverName}

discord-watchlist-connection-entry = - {$playerName} with message "{$message}"
discord-watchlist-connection-entry-expires = - {$playerName} with message "{$message}" (expires <t:{$expiry}:R>)
discord-watchlist-connection-entry-more = - {$playerName} with message "{$message}" and { $otherWatchlists ->
        [one] {$otherWatchlists} other watchlist
        *[other] {$otherWatchlists} other watchlists
    }
discord-watchlist-connection-entry-expires-more = - {$playerName} with message "{$message}" (expires <t:{$expiry}:R>) and { $otherWatchlists ->
        [one] {$otherWatchlists} other watchlist
        *[other] {$otherWatchlists} other watchlists
    }
