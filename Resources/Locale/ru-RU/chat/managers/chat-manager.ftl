### UI

chat-manager-max-message-length = Ваше сообщение превышает лимит в {$maxMessageLength} символов
chat-manager-ooc-chat-enabled-message = OOC чат был включен.
chat-manager-ooc-chat-disabled-message = OOC чат был отключен.
chat-manager-looc-chat-enabled-message = LOOC чат был включен.
chat-manager-looc-chat-disabled-message = LOOC чат был отключен.
chat-manager-dead-looc-chat-enabled-message = Мёртвые игроки теперь могут говорить в LOOC.
chat-manager-dead-looc-chat-disabled-message = Мёртвые игроки больше не могут говорить в LOOC.
chat-manager-crit-looc-chat-enabled-message = Игроки с критическим уроном теперь могут использовать LOOC.
chat-manager-crit-looc-chat-disabled-message = Игроки с критическим уроном больше не могут использовать LOOC.
chat-manager-admin-ooc-chat-enabled-message = Админ OOC чат был включен.
chat-manager-admin-ooc-chat-disabled-message = Админ OOC чат был выключен.

chat-manager-max-message-length-exceeded-message = Ваше сообщение превышает лимит в {$limit} символов
chat-manager-no-headset-on-message = У вас нет гарнитуры!
chat-manager-no-radio-key = Не задан ключ канала!
chat-manager-no-such-channel = Нет канала с ключём '{$key}'!
chat-manager-whisper-headset-on-message = Вы не можете шептать в радио!

chat-manager-server-wrap-message = СЕРВЕР: {$message}
chat-manager-sender-announcement-wrap-message = {$sender} Объявление:
                                                {$message}
chat-manager-entity-say-wrap-message = {$entityName} говорит, "{$message}"
chat-manager-entity-say-bold-wrap-message = [bold]{$entityName}[/bold] {$verb}, [font={$fontType} size={$fontSize}][bold]"{$message}"[/bold][/font]

chat-manager-entity-whisper-wrap-message = [font size=11][italic]{$entityName} шепчет, "{$message}"[/italic][/font]
chat-manager-entity-whisper-unknown-wrap-message = [font size=11][italic]Someone шепчет, "{$message}"[/italic][/font]

# THE() is not used here because the entity and its name can technically be disconnected if a nameOverride is passed...
chat-manager-entity-me-wrap-message = [italic]{ PROPER($entity) ->
    *[false] {$entityName} {$message}[/italic]
     [true] {$entityName} {$message}[/italic]
    }

chat-manager-entity-looc-wrap-message = LOOC: [bold]{$entityName}:[/bold] {$message}
chat-manager-send-ooc-wrap-message = OOC: [bold]{$playerName}:[/bold] {$message}
chat-manager-send-ooc-patron-wrap-message = OOC: [bold][color={$patronColor}]{$playerName}[/color]:[/bold] {$message}
chat-manager-entity-looc-patron-wrap-message = LOOC: [bold][color={$patronColor}]{$entityName}[/color]:[/bold] {$message}

chat-manager-send-dead-chat-wrap-message = {$deadChannelName}: [bold]{$playerName}:[/bold] {$message}
chat-manager-send-admin-dead-chat-wrap-message = {$adminChannelName}: [bold]({$userName}):[/bold] {$message}
chat-manager-send-admin-chat-wrap-message = {$adminChannelName}: [bold]{$playerName}:[/bold] {$message}
chat-manager-send-admin-announcement-wrap-message = [bold]{$adminChannelName}: {$message}[/bold]

chat-manager-send-hook-ooc-wrap-message = OOC: [bold](D){$senderName}:[/bold] {$message}

chat-manager-dead-channel-name = МЁРТВЫЕ
chat-manager-admin-channel-name = АДМИН

## Speech verbs for chat

chat-speech-verb-suffix-exclamation = !
chat-speech-verb-suffix-exclamation-strong = !!
chat-speech-verb-suffix-question = ?
chat-speech-verb-suffix-stutter = -
chat-speech-verb-suffix-mumble = ..

chat-speech-verb-default = говорит
chat-speech-verb-exclamation = восклицает
chat-speech-verb-exclamation-strong = кричит
chat-speech-verb-question = спрашивает
chat-speech-verb-stutter = заикается
chat-speech-verb-mumble = бормочет

chat-speech-verb-insect-1 = болтает
chat-speech-verb-insect-2 = щебечет
chat-speech-verb-insect-3 = щелкает

chat-speech-verb-winged-1 = порхает
chat-speech-verb-winged-2 = замахивает
chat-speech-verb-winged-3 = жужжит

chat-speech-verb-slime-1 = плескается
chat-speech-verb-slime-2 = булькает
chat-speech-verb-slime-3 = сочится

chat-speech-verb-plant-1 = шуршит
chat-speech-verb-plant-2 = качается
chat-speech-verb-plant-3 = скрипит

chat-speech-verb-robotic-1 = стоит
chat-speech-verb-robotic-2 = бипает

chat-speech-verb-reptilian-1 = шипит
chat-speech-verb-reptilian-2 = фыркает
chat-speech-verb-reptilian-3 = раздражается

chat-speech-verb-skeleton-1 = гремит
chat-speech-verb-skeleton-2 = щелкает
chat-speech-verb-skeleton-3 = скрежит

chat-speech-verb-canine-1 = лает
chat-speech-verb-canine-2 = гавкает
chat-speech-verb-canine-3 = воет

chat-speech-verb-small-mob-1 = пищит
chat-speech-verb-small-mob-2 = пипает

chat-speech-verb-large-mob-1 = ревет
chat-speech-verb-large-mob-2 = рычит

chat-speech-verb-monkey-1 = умничает
chat-speech-verb-monkey-2 = визгает

chat-speech-verb-cluwne-1 = хихикает
chat-speech-verb-cluwne-2 = хохочет
chat-speech-verb-cluwne-3 = ржёт

chat-speech-verb-ghost-1 = complains
chat-speech-verb-ghost-2 = breathes
chat-speech-verb-ghost-3 = hums
chat-speech-verb-ghost-4 = mutters
