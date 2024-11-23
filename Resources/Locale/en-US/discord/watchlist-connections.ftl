discord-watchlist-connection-header =
    { $players ->
        [one] {$players} player on a watchlist has
        *[other] {$players} players on a watchlist have
    } connected to {$serverName}

discord-watchlist-connection-entry = - {$playerName}

discord-watchlist-connection-no-expiry = (permanent)
discord-watchlist-connection-expiry = (expires <t:{$expiry}:R>)

discord-watchlist-connection-message = with message "{$message}"

discord-watchlist-connection-more = (and
    { $watchlists ->
        [one] {$watchlists} other watchlist
        *[other] {$watchlists} other watchlists
    })
