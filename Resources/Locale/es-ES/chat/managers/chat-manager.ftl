### UI

chat-manager-max-message-length = Tu mensaje excede el límite de {$maxMessageLength} caracteres.
chat-manager-ooc-chat-enabled-message = El chat OOC ha sido habilitado.
chat-manager-ooc-chat-disabled-message = El chat OOC ha sido deshabilitado.
chat-manager-looc-chat-enabled-message = El chat LOOC ha sido habilitado.
chat-manager-looc-chat-disabled-message = El chat LOOC ha sido deshabilitado.
chat-manager-dead-looc-chat-enabled-message = Los jugadores muertos ahora pueden usar LOOC.
chat-manager-dead-looc-chat-disabled-message = Los jugadores muertos ya no pueden usar LOOC.
chat-manager-crit-looc-chat-enabled-message = Los jugadores en crítico ahora pueden usar LOOC.
chat-manager-crit-looc-chat-disabled-message = Los jugadores en crítico ya no pueden usar LOOC.
chat-manager-admin-ooc-chat-enabled-message = El chat OOC para administradores ha sido habilitado.
chat-manager-admin-ooc-chat-disabled-message = El chat OOC para administradores ha sido deshabilitado.

chat-manager-max-message-length-exceeded-message = Tu mensaje excedió el límite de {$limit} caracteres.
chat-manager-no-headset-on-message = ¡No tienes un auricular puesto!
chat-manager-no-radio-key = ¡No se especificó una clave de radio!
chat-manager-no-such-channel = No existe un canal con la clave '{$key}'.
chat-manager-whisper-headset-on-message = ¡No puedes susurrar por radio!

chat-manager-server-wrap-message = [bold]{$message}[/bold]
chat-manager-sender-announcement = Central Command
chat-manager-sender-announcement-wrap-message = [font size=14][bold]{$sender} Anuncio:[/font][font size=12]
                                                {$message}[/bold][/font]
chat-manager-entity-say-wrap-message = [BubbleHeader][bold][Nombre]{$entityName}[/Nombre][/bold][/BubbleHeader] {$verb}, [font={$fontType} size={$fontSize}]"[BubbleContent]{$message}[/BubbleContent]"[/font]
chat-manager-entity-say-bold-wrap-message = [BubbleHeader][bold][Nombre]{$entityName}[/Nombre][/bold][/BubbleHeader] {$verb}, [font={$fontType} size={$fontSize}]"[BubbleContent][bold]{$message}[/bold][/BubbleContent]"[/font]

chat-manager-entity-whisper-wrap-message = [font size=11][italic][BubbleHeader][Nombre]{$entityName}[/Nombre][/BubbleHeader] susurra,"[BubbleContent]{$message}[/BubbleContent]"[/italic][/font]
chat-manager-entity-whisper-unknown-wrap-message = [font size=11][italic][BubbleHeader]Alguien[/BubbleHeader] susurra, "[BubbleContent]{$message}[/BubbleContent]"[/italic][/font]

chat-manager-entity-me-wrap-message = [italic]{ PROPER($entity) ->
    *[false] El {$entityName} {$message}[/italic]
     [true] {CAPITALIZE($entityName)} {$message}[/italic]
    }

chat-manager-entity-looc-wrap-message = LOOC: [bold]{$entityName}:[/bold] {$message}
chat-manager-send-ooc-wrap-message = OOC: [bold]{$playerName}:[/bold] {$message}
chat-manager-send-ooc-patron-wrap-message = OOC: [bold][color={$patronColor}]{$playerName}[/color]:[/bold] {$message}

chat-manager-send-dead-chat-wrap-message = {$deadChannelName}: [bold][BubbleHeader]{$playerName}[/BubbleHeader]:[/bold] [BubbleContent]{$message}[/BubbleContent]
chat-manager-send-admin-dead-chat-wrap-message = {$adminChannelName}: [bold]([BubbleHeader]{$userName}[/BubbleHeader]):[/bold] [BubbleContent]{$message}[/BubbleContent]
chat-manager-send-admin-chat-wrap-message = {$adminChannelName}: [bold]{$playerName}:[/bold] {$message}
chat-manager-send-admin-announcement-wrap-message = [bold]{$adminChannelName}: {$message}[/bold]

chat-manager-send-hook-ooc-wrap-message = OOC: [bold](D){$senderName}:[/bold] {$message}

chat-manager-dead-channel-name = MUERTO
chat-manager-admin-channel-name = ADMIN

chat-manager-rate-limited = ¡Estás enviando mensajes demasiado rápido!
chat-manager-rate-limit-admin-announcement = El jugador { $player } ha superado los límites de velocidad de chat. Vigílalo si esto ocurre con frecuencia.

## Verbos de habla para el chat

chat-speech-verb-suffix-exclamation = ¡
chat-speech-verb-suffix-exclamation-strong = ¡¡
chat-speech-verb-suffix-question = ?
chat-speech-verb-suffix-stutter = -
chat-speech-verb-suffix-mumble = ..

chat-speech-verb-name-none = Ninguno
chat-speech-verb-name-default = Predeterminado
chat-speech-verb-default = dice
chat-speech-verb-name-exclamation = Exclamando
chat-speech-verb-exclamation = exclama
chat-speech-verb-name-exclamation-strong = Gritando
chat-speech-verb-exclamation-strong = grita
chat-speech-verb-name-question = Preguntando
chat-speech-verb-question = pregunta
chat-speech-verb-name-stutter = Tartamudeando
chat-speech-verb-stutter = tartamudea
chat-speech-verb-name-mumble = Murmurando
chat-speech-verb-mumble = murmura

chat-speech-verb-name-arachnid = Araña
chat-speech-verb-insect-1 = chasquea
chat-speech-verb-insect-2 = chirría
chat-speech-verb-insect-3 = clickea

chat-speech-verb-name-moth = Polilla
chat-speech-verb-winged-1 = aletea
chat-speech-verb-winged-2 = agita
chat-speech-verb-winged-3 = zumba

chat-speech-verb-name-slime = Limosa
chat-speech-verb-slime-1 = chapotea
chat-speech-verb-slime-2 = burbujea
chat-speech-verb-slime-3 = rezuma

chat-speech-verb-name-plant = Diona
chat-speech-verb-plant-1 = susurra
chat-speech-verb-plant-2 = se balancea
chat-speech-verb-plant-3 = cruje

chat-speech-verb-name-robotic = Robótico
chat-speech-verb-robotic-1 = declara
chat-speech-verb-robotic-2 = emite pitidos
chat-speech-verb-robotic-3 = hace beep

chat-speech-verb-name-reptilian = Reptil
chat-speech-verb-reptilian-1 = sisea
chat-speech-verb-reptilian-2 = resopla
chat-speech-verb-reptilian-3 = exhala

chat-speech-verb-name-skeleton = Esqueleto
chat-speech-verb-skeleton-1 = tintinea
chat-speech-verb-skeleton-2 = chasquea
chat-speech-verb-skeleton-3 = rechina

chat-speech-verb-name-vox = Vox
chat-speech-verb-vox-1 = chirría
chat-speech-verb-vox-2 = grita
chat-speech-verb-vox-3 = croa

chat-speech-verb-name-canine = Canino
chat-speech-verb-canine-1 = ladra
chat-speech-verb-canine-2 = gime
chat-speech-verb-canine-3 = aúlla

chat-speech-verb-name-small-mob = Ratón
chat-speech-verb-small-mob-1 = squeaks
chat-speech-verb-small-mob-2 = pieps

chat-speech-verb-name-large-mob = Carpa
chat-speech-verb-large-mob-1 = ruge
chat-speech-verb-large-mob-2 = gruñe

chat-speech-verb-name-monkey = Mono
chat-speech-verb-monkey-1 = chimp
chat-speech-verb-monkey-2 = grita

chat-speech-verb-name-cluwne = Cluwne

chat-speech-verb-name-parrot = Loro
chat-speech-verb-parrot-1 = grazia
chat-speech-verb-parrot-2 = tuita
chat-speech-verb-parrot-3 = chirría

chat-speech-verb-cluwne-1 = ríe
chat-speech-verb-cluwne-2 = se descojona
chat-speech-verb-cluwne-3 = se ríe

chat-speech-verb-name-ghost = Fantasma
chat-speech-verb-ghost-1 = se queja
chat-speech-verb-ghost-2 = respira
chat-speech-verb-ghost-3 = tararea
chat-speech-verb-ghost-4 = murmura

chat-speech-verb-name-electricity = Electricidad
chat-speech-verb-electricity-1 = chisporrotea
chat-speech-verb-electricity-2 = zumba
chat-speech-verb-electricity-3 = chirría
