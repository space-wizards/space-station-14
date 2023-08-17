### UI

chat-manager-max-message-length = Your message exceeds {$maxMessageLength} character limit
chat-manager-ooc-chat-enabled-message = OOC chat has been enabled.
chat-manager-ooc-chat-disabled-message = OOC chat has been disabled.
chat-manager-looc-chat-enabled-message = LOOC chat has been enabled.
chat-manager-looc-chat-disabled-message = LOOC chat has been disabled.
chat-manager-dead-looc-chat-enabled-message = Dead players can now use LOOC.
chat-manager-dead-looc-chat-disabled-message = Dead players can no longer use LOOC.
chat-manager-crit-looc-chat-enabled-message = Crit players can now use LOOC.
chat-manager-crit-looc-chat-disabled-message = Crit players can no longer use LOOC.
chat-manager-admin-ooc-chat-enabled-message = Admin OOC chat has been enabled.
chat-manager-admin-ooc-chat-disabled-message = Admin OOC chat has been disabled.

chat-manager-max-message-length-exceeded-message = Your message exceeded {$limit} character limit
chat-manager-no-headset-on-message = You don't have a headset on!
chat-manager-no-radio-key = No radio key specified!
chat-manager-no-such-channel = There is no channel with key '{$key}'!
chat-manager-whisper-headset-on-message = You can't whisper on the radio!

chat-manager-server-wrap-message = [bold]{$message}[/bold]
chat-manager-sender-announcement-wrap-message = [font size=14][bold]{$sender} Announcement:[/font][font size=12]
                                                {$message}[/bold][/font]
chat-manager-entity-say-wrap-message = [bold]{$entityName}[/bold] {$verb}, [font={$fontType} size={$fontSize}]"{$message}"[/font]
chat-manager-entity-say-bold-wrap-message = [bold]{$entityName}[/bold] {$verb}, [font={$fontType} size={$fontSize}][bold]"{$message}"[/bold][/font]

chat-manager-entity-whisper-wrap-message = [font size=11][italic]{$entityName} whispers, "{$message}"[/italic][/font]
chat-manager-entity-whisper-unknown-wrap-message = [font size=11][italic]Someone whispers, "{$message}"[/italic][/font]

# THE() is not used here because the entity and its name can technically be disconnected if a nameOverride is passed...
chat-manager-entity-me-wrap-message = [italic]{ PROPER($entity) ->
    *[false] the {$entityName} {$message}[/italic]
     [true] {$entityName} {$message}[/italic]
    }

chat-manager-entity-looc-wrap-message = LOOC: [bold]{$entityName}:[/bold] {$message}
chat-manager-send-ooc-wrap-message = OOC: [bold]{$playerName}:[/bold] {$message}
chat-manager-send-ooc-patron-wrap-message = OOC: [bold][color={$patronColor}]{$playerName}[/color]:[/bold] {$message}

chat-manager-send-dead-chat-wrap-message = {$deadChannelName}: [bold]{$playerName}:[/bold] {$message}
chat-manager-send-admin-dead-chat-wrap-message = {$adminChannelName}: [bold]({$userName}):[/bold] {$message}
chat-manager-send-admin-chat-wrap-message = {$adminChannelName}: [bold]{$playerName}:[/bold] {$message}
chat-manager-send-admin-announcement-wrap-message = [bold]{$adminChannelName}: {$message}[/bold]

chat-manager-send-hook-ooc-wrap-message = OOC: [bold](D){$senderName}:[/bold] {$message}

chat-manager-dead-channel-name = DEAD
chat-manager-admin-channel-name = ADMIN

## Speech verbs for chat

chat-speech-verb-suffix-exclamation = !
chat-speech-verb-suffix-exclamation-strong = !!
chat-speech-verb-suffix-question = ?

chat-speech-verb-default = says
chat-speech-verb-exclamation = shouts
chat-speech-verb-exclamation-strong = yells
chat-speech-verb-question = asks

chat-speech-verb-insect-1 = chitters
chat-speech-verb-insect-2 = flutters

chat-speech-verb-slime = chirps

chat-speech-verb-robotic = states
chat-speech-verb-reptilian = hisses

chat-speech-verb-skeleton = rattles

chat-speech-verb-small-mob = squeaks

chat-speech-verb-large-mob = roars

chat-speech-verb-monkey = chimpers

chat-speech-verb-cluwne-1 = giggles
chat-speech-verb-cluwne-2 = guffaws
chat-speech-verb-cluwne-3 = laughs
