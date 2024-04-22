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
chat-manager-entity-say-wrap-message = [BubbleHeader][bold][Name]{$entityName}[/Name][/bold][/BubbleHeader] {$verb}, [font={$fontType} size={$fontSize}]"[BubbleContent]{$message}[/BubbleContent]"[/font]
chat-manager-entity-say-bold-wrap-message = [BubbleHeader][bold][Name]{$entityName}[/Name][/bold][/BubbleHeader] {$verb}, [font={$fontType} size={$fontSize}]"[BubbleContent][bold]{$message}[/bold][/BubbleContent]"[/font]

chat-manager-entity-whisper-wrap-message = [font size=11][italic][BubbleHeader][Name]{$entityName}[/Name][/BubbleHeader] whispers,"[BubbleContent]{$message}[/BubbleContent]"[/italic][/font]
chat-manager-entity-whisper-unknown-wrap-message = [font size=11][italic][BubbleHeader]Someone[/BubbleHeader] whispers, "[BubbleContent]{$message}[/BubbleContent]"[/italic][/font]

# THE() is not used here because the entity and its name can technically be disconnected if a nameOverride is passed...
chat-manager-entity-me-wrap-message = [italic]{ PROPER($entity) ->
    *[false] the {$entityName} {$message}[/italic]
     [true] {$entityName} {$message}[/italic]
    }

chat-manager-entity-looc-wrap-message = LOOC: [bold]{$entityName}:[/bold] {$message}
chat-manager-send-ooc-wrap-message = OOC: [bold]{$playerName}:[/bold] {$message}
chat-manager-send-ooc-patron-wrap-message = OOC: [bold][color={$patronColor}]{$playerName}[/color]:[/bold] {$message}

chat-manager-send-dead-chat-wrap-message = {$deadChannelName}: [bold][BubbleHeader]{$playerName}[/BubbleHeader]:[/bold] [BubbleContent]{$message}[/BubbleContent]
chat-manager-send-admin-dead-chat-wrap-message = {$adminChannelName}: [bold]([BubbleHeader]{$userName}[/BubbleHeader]):[/bold] [BubbleContent]{$message}[/BubbleContent]
chat-manager-send-admin-chat-wrap-message = {$adminChannelName}: [bold]{$playerName}:[/bold] {$message}
chat-manager-send-admin-announcement-wrap-message = [bold]{$adminChannelName}: {$message}[/bold]

chat-manager-send-hook-ooc-wrap-message = OOC: [bold](D){$senderName}:[/bold] {$message}

chat-manager-dead-channel-name = DEAD
chat-manager-admin-channel-name = ADMIN

chat-manager-rate-limited = You are sending messages too quickly!
chat-manager-rate-limit-admin-announcement = Player { $player } breached chat rate limits. Watch them if this is a regular occurence.

## Speech verbs for chat

chat-speech-verb-suffix-exclamation = !
chat-speech-verb-suffix-exclamation-strong = !!
chat-speech-verb-suffix-question = ?
chat-speech-verb-suffix-stutter = -
chat-speech-verb-suffix-mumble = ..

chat-speech-verb-name-none = None
chat-speech-verb-name-default = Default
chat-speech-verb-default = says
chat-speech-verb-name-exclamation = Exclaiming
chat-speech-verb-exclamation = exclaims
chat-speech-verb-name-exclamation-strong = Yelling
chat-speech-verb-exclamation-strong = yells
chat-speech-verb-name-question = Asking
chat-speech-verb-question = asks
chat-speech-verb-name-stutter = Stuttering
chat-speech-verb-stutter = stutters
chat-speech-verb-name-mumble = Mumbling
chat-speech-verb-mumble = mumbles

chat-speech-verb-name-arachnid = Arachnid
chat-speech-verb-insect-1 = chitters
chat-speech-verb-insect-2 = chirps
chat-speech-verb-insect-3 = clicks

chat-speech-verb-name-moth = Moth
chat-speech-verb-winged-1 = flutters
chat-speech-verb-winged-2 = flaps
chat-speech-verb-winged-3 = buzzes

chat-speech-verb-name-slime = Slime
chat-speech-verb-slime-1 = sloshes
chat-speech-verb-slime-2 = burbles
chat-speech-verb-slime-3 = oozes

chat-speech-verb-name-plant = Diona
chat-speech-verb-plant-1 = rustles
chat-speech-verb-plant-2 = sways
chat-speech-verb-plant-3 = creaks

chat-speech-verb-name-robotic = Robotic
chat-speech-verb-robotic-1 = states
chat-speech-verb-robotic-2 = beeps
chat-speech-verb-robotic-3 = boops

chat-speech-verb-name-reptilian = Reptilian
chat-speech-verb-reptilian-1 = hisses
chat-speech-verb-reptilian-2 = snorts
chat-speech-verb-reptilian-3 = huffs

chat-speech-verb-name-skeleton = Skeleton
chat-speech-verb-skeleton-1 = rattles
chat-speech-verb-skeleton-2 = clacks
chat-speech-verb-skeleton-3 = gnashes

chat-speech-verb-name-vox = Vox
chat-speech-verb-vox-1 = screeches
chat-speech-verb-vox-2 = shrieks
chat-speech-verb-vox-3 = croaks

chat-speech-verb-name-canine = Canine
chat-speech-verb-canine-1 = barks
chat-speech-verb-canine-2 = woofs
chat-speech-verb-canine-3 = howls

chat-speech-verb-name-small-mob = Mouse
chat-speech-verb-small-mob-1 = squeaks
chat-speech-verb-small-mob-2 = pieps

chat-speech-verb-name-large-mob = Carp
chat-speech-verb-large-mob-1 = roars
chat-speech-verb-large-mob-2 = growls

chat-speech-verb-name-monkey = Monkey
chat-speech-verb-monkey-1 = chimpers
chat-speech-verb-monkey-2 = screeches

chat-speech-verb-name-cluwne = Cluwne

chat-speech-verb-name-parrot = Parrot
chat-speech-verb-parrot-1 = squawks
chat-speech-verb-parrot-2 = tweets
chat-speech-verb-parrot-3 = chirps

chat-speech-verb-cluwne-1 = giggles
chat-speech-verb-cluwne-2 = guffaws
chat-speech-verb-cluwne-3 = laughs

chat-speech-verb-name-ghost = Ghost
chat-speech-verb-ghost-1 = complains
chat-speech-verb-ghost-2 = breathes
chat-speech-verb-ghost-3 = hums
chat-speech-verb-ghost-4 = mutters

chat-speech-verb-name-electricity = Electricity
chat-speech-verb-electricity-1 = crackles
chat-speech-verb-electricity-2 = buzzes
chat-speech-verb-electricity-3 = screeches
