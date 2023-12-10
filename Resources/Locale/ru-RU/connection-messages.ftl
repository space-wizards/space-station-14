whitelist-not-whitelisted = Вас нет в вайтлисте.
# proper handling for having a min/max or not
whitelist-playercount-invalid =
    { $min ->
        [0] Вайтлист для этого сервера применяется только для числа игроков ниже { $max }.
       *[other]
            Вайтлист для этого сервера применяется только для числа игроков выше { $min } { $max ->
                [2147483647] ->  так что, возможно, вы сможете присоединиться позже.
               *[other] ->  и ниже { $max } игроков, так что, возможно, вы сможете присоединиться позже.
            }
    }
whitelist-not-whitelisted-rp = Вас нет в вайтлисте. Чтобы попасть в вайтлист, посетите наш Discord (ссылку можно найти в описании сервера).
cmd-whitelistadd-description = Добавить игрока с указанным юзернеймом в вайтлист.
cmd-whitelistadd-help = whitelistadd <username>
cmd-whitelistadd-existing = { $username } уже в вайтлисте!
cmd-whitelistadd-added = { $username } добавлен в вайтлист
cmd-whitelistadd-not-found = Пользователь '{ $username }' не найден
cmd-whitelistremove-description = Удалить игрока с указанным юзернеймом из вайтлиста.
cmd-whitelistremove-help = whitelistremove <username>
cmd-whitelistremove-existing = { $username } не в вайтлисте!
cmd-whitelistremove-removed = Пользователь { $username } удалён из вайтлиста
cmd-whitelistremove-not-found = Пользователь '{ $username }' не найден
cmd-kicknonwhitelisted-description = Кикнуть с сервера всех пользователей не из вайтлиста.
cmd-kicknonwhitelisted-help = kicknonwhitelisted
ban-banned-permanent = Этот бан можно только обжаловать. Для этого посетите наш Discord (ссылку можно найти в описании сервера).
ban-banned-permanent-appeal = Этот бан можно только обжаловать. Для этого посетите { $link }
ban-expires = Вы получили бан на { $duration } минут, и он истечёт { $time } по UTC (для московского времени добавьте 3 часа).
ban-banned-1 = Вам, или другому пользователю этого компьютера или соединения, запрещено здесь играть.
ban-banned-2 = Причина бана: "{ $reason }"
ban-banned-3 = Попытки обойти этот бан, например, путём создания нового аккаунта, будут фиксироваться.
soft-player-cap-full = Сервер заполнен!
panic-bunker-account-denied = Этот сервер находится в режиме "Бункер", часто используемом в целях борьбы с рейдами. Новые подключения, не удовлетворяющие правилам, не принимаются. Повторите попытку позже
panic-bunker-account-denied-reason = Этот сервер находится в режиме "Бункер", часто используемом в целях борьбы с рейдами. Новые подключения, не удовлетворяющие правилам, не принимаются. Повторите попытку позже Причина: "{ $reason }"
panic-bunker-account-reason-account = Ваш аккаунт должен быть старше { $minutes } минут
panic-bunker-account-reason-overall =
    Необходимо минимальное отыгранное время — { $hours } { $hours ->
        [one] час
        [few] часа
       *[other] часов
    }.
