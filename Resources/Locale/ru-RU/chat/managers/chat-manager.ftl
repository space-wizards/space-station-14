### UI

chat-manager-max-message-length = Ваше сообщение превышает лимит в { $maxMessageLength } символов
chat-manager-ooc-chat-enabled-message = OOC чат был включен.
chat-manager-ooc-chat-disabled-message = OOC чат был отключен.
chat-manager-looc-chat-enabled-message = LOOC чат был включен.
chat-manager-looc-chat-disabled-message = LOOC чат был отключен.
chat-manager-dead-looc-chat-enabled-message = Мёртвые игроки теперь могут говорить в LOOC.
chat-manager-dead-looc-chat-disabled-message = Мёртвые игроки больше не могут говорить в LOOC.
chat-manager-crit-looc-chat-enabled-message = Игроки в критическом состоянии теперь могут говорить в LOOC.
chat-manager-crit-looc-chat-disabled-message = Игроки в критическом состоянии больше не могут говорить в LOOC.
chat-manager-admin-ooc-chat-enabled-message = Админ OOC чат был включен.
chat-manager-admin-ooc-chat-disabled-message = Админ OOC чат был выключен.

chat-manager-max-message-length-exceeded-message = Ваше сообщение превышает лимит в { $limit } символов
chat-manager-no-headset-on-message = У вас нет гарнитуры!
chat-manager-no-radio-key = Не задан ключ канала!
chat-manager-no-such-channel = Нет канала с ключём '{ $key }'!
chat-manager-whisper-headset-on-message = Вы не можете шептать в радио!

chat-manager-server-wrap-message = [bold]{$message}[/bold]
chat-manager-sender-announcement-wrap-message = [font size=14][bold]Объявление {$sender}:[/font][font size=12][bold]
                                                {$message}[/bold][/font]
chat-manager-sender-announcement-highlight-wrap-message = [font size=14][bold]Объявление {$sender}:
                                                {$message}[/font]
chat-manager-entity-say-wrap-message = [BubbleHeader][bold]{$entityName}[/bold][/BubbleHeader] {$verb}, [font={$fontType} size={$fontSize}]"[BubbleContent]{$message}[/BubbleContent]"[/font]
chat-manager-entity-say-bold-wrap-message = [BubbleHeader][bold]{$entityName}[/bold][/BubbleHeader] {$verb}, [font={$fontType} size={$fontSize}]"[BubbleContent][bold]{$message}[/bold][/BubbleContent]"[/font]

chat-manager-entity-whisper-wrap-message = [font size=11][italic][BubbleHeader]{$entityName}[/BubbleHeader] шепчет, "[BubbleContent]{$message}[/BubbleContent]"[/italic][/font]
chat-manager-entity-whisper-unknown-wrap-message = [font size=11][italic][BubbleHeader]Кто-то[/BubbleHeader] шепчет, "[BubbleContent]{$message}[/BubbleContent]"[/italic][/font]
chat-manager-entity-me-wrap-message = [italic]{ PROPER($entity) ->
    *[false] {$entityName} {$message}[/italic]
     [true] {$entityName} {$message}[/italic]
    }
chat-manager-entity-looc-wrap-message = LOOC: [bold]{$entityName}:[/bold] {$message}
chat-manager-send-ooc-wrap-message = OOC: [bold]{$playerName}:[/bold] {$message}
chat-manager-send-sponsor-ooc-wrap-message = OOC: ★ [bold]{$playerName}:[/bold] {$message}
chat-manager-send-host-ooc-wrap-message = OOC: ➤ [bold]{$playerName}: {$message}[/bold]
chat-manager-send-ooc-patron-wrap-message = OOC: [bold][color={$patronColor}]{$playerName}[/color]:[/bold] {$message}
chat-manager-send-dead-chat-wrap-message = {$deadChannelName}: [bold]{$playerName}:[/bold] {$message}
chat-manager-send-admin-dead-chat-wrap-message = {$adminChannelName}: [bold]({$userName}):[/bold] {$message}
chat-manager-send-admin-chat-wrap-message = {$adminChannelName}: [bold]{$playerName}:[/bold] {$message}
chat-manager-send-admin-announcement-wrap-message = [bold]{$adminChannelName}: {$message}[/bold]
chat-manager-send-hook-ooc-wrap-message = OOC: [bold](D){$senderName}:[/bold] {$message}
chat-manager-dead-channel-name = МЁРТВЫЕ
chat-manager-admin-channel-name = АДМИН

chat-manager-rate-limited = Вы посылаете сообщения слишком быстро!
chat-manager-rate-limit-admin-announcement = Игрок { $player } превысили ограничение скорости отправления сообщений.


## Speech verbs for chat

chat-speech-verb-suffix-exclamation = !
chat-speech-verb-suffix-exclamation-strong = !!
chat-speech-verb-suffix-question = ?
chat-speech-verb-suffix-stutter = -
chat-speech-verb-suffix-mumble = ..

chat-speech-verb-default = говорит
chat-speech-verb-exclamation = заявляет
chat-speech-verb-exclamation-strong = орёт
chat-speech-verb-question = спрашивает
chat-speech-verb-stutter = запинается
chat-speech-verb-mumble = бормочет

chat-speech-verb-insect-1 = стрекочет
chat-speech-verb-insect-2 = жужжит
chat-speech-verb-insect-3 = щёлкает

chat-speech-verb-winged-1 = стрекочет
chat-speech-verb-winged-2 = жужжит
chat-speech-verb-winged-3 = щёлкает

chat-speech-verb-slime-1 = булькает
chat-speech-verb-slime-2 = хлюпает
chat-speech-verb-slime-3 = урчит

chat-speech-verb-plant-1 = шуршит
chat-speech-verb-plant-2 = шелестит
chat-speech-verb-plant-3 = скрипит

chat-speech-verb-robotic-1 = констатирует
chat-speech-verb-robotic-2 = гудит

chat-speech-verb-reptilian-1 = шипит
chat-speech-verb-reptilian-2 = фыркает
chat-speech-verb-reptilian-3 = пыхтит

chat-speech-verb-skeleton-1 = гремит
chat-speech-verb-skeleton-2 = трещит
chat-speech-verb-skeleton-3 = скрежещет

chat-speech-verb-canine-1 = гавкает
chat-speech-verb-canine-2 = тяфкает
chat-speech-verb-canine-3 = воет

chat-speech-verb-small-mob-1 = пищит
chat-speech-verb-small-mob-2 = пыхтит

chat-speech-verb-large-mob-1 = рычит
chat-speech-verb-large-mob-2 = скалится

chat-speech-verb-large-mob = рычит

chat-speech-verb-monkey-1 = визжит
chat-speech-verb-monkey-2 = кричит

chat-speech-verb-cluwne-1 = хихикает
chat-speech-verb-cluwne-2 = хохочет
chat-speech-verb-cluwne-3 = смеется

chat-speech-verb-ghost-1 = ропщет
chat-speech-verb-ghost-2 = вздыхает
chat-speech-verb-ghost-3 = напевает
chat-speech-verb-ghost-4 = бурчит
