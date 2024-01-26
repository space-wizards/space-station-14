whitelist-not-whitelisted = Ви не у вайтлісті.

# proper handling for having a min/max or not
whitelist-playercount-invalid = {$min ->
    [0] Вайтліст на цьому сервері працює тільки коли гравців менше за {$max}.
    *[other] Вайтліст на цьому сервері працює тільки коли гравців більше за {$min} {$max ->
        [2147483647] -> гравців, так що ви зможете під'єднатися пізніше.
       *[other] -> гравців і менше за {$max} гравців, так що ви зможете під'єднатися пізніше.
    }
}
whitelist-not-whitelisted-rp = Ви не у вайтлісті. Щоб потрапити у нього, посетить наш Діскорд

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

ban-banned-permanent = Цей бан може бути знятий через апіляцію.
ban-banned-permanent-appeal = Цей бан може бути знятий тільки через апіляцію. Ви можете подати її на {$link}
ban-expires = Цей бан продлиться ще {$duration} хвилин та пройде {$time} по UTC.
ban-banned-1 = Ви, або інший гравець на цьому комп'ютері чи з'єднані, були забанені на цьому сервері.
ban-banned-2 = Причина бана: "{$reason}"
ban-banned-3 = Спроби обійти цей бан, наприклад шляхом зроблення нового акаунту, будуть зафіксовані.

soft-player-cap-full = Цій сервер повний!
panic-bunker-account-denied = Цей сервер у режиму Панік бункера. Нові з'єднаня не приймаються у даний час. Спробуйте знов пізніше
panic-bunker-account-denied-reason = Цей сервер у режиму Панік бункера й вам було відмовлено в під'єднані. Причина: "{$reason}"
panic-bunker-account-reason-account = Вік цього акаунту повинен бути більше {$minutes} хвилин.
panic-bunker-account-reason-overall = Загальний час гри на цьому акаунті повинен бути більше {$hours} годин
