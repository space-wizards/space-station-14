
## Traitor

# Shown at the end of a round of Traitor
traitor-round-end-result = {$traitorCount ->
    [one] Havia um traidor.
    *[other] Haviam {$traitorCount} traidores.
}

# Shown at the end of a round of Traitor
traitor-user-was-a-traitor = [color=gray]{$user}[/color] era um traidor.
traitor-user-was-a-traitor-named = [color=White]{$name}[/color] ([color=gray]{$user}[/color]) era um traidor.
traitor-was-a-traitor-named = [color=White]{$name}[/color] era um traidor.

traitor-user-was-a-traitor-with-objectives = [color=gray]{$user}[/color] era um traidor com os seguintes objetivos:
traitor-user-was-a-traitor-with-objectives-named = [color=White]{$name}[/color] ([color=gray]{$user}[/color]) era um traidor que tinha como objetivos:
traitor-was-a-traitor-with-objectives-named = [color=White]{$name}[/color] era um traidor que tinha os seguintes objetivos:

preset-traitor-objective-issuer-syndicate = [color=#87cefa]The Syndicate[/color]

# Shown at the end of a round of Traitor
traitor-objective-condition-success = {$condition} | [color={$markupColor}]Sucesso![/color]

# Shown at the end of a round of Traitor
traitor-objective-condition-fail = {$condition} | [color={$markupColor}]Falhou![/color] ({$progress}%)

traitor-title = Traitor
traitor-description = Há traidores entre nós...
traitor-not-enough-ready-players = Faltou mais jogadores prontos para a partida! Haviam {$readyPlayersCount} jogadores prontos de {$minimumPlayers} necessários.
traitor-no-one-ready = Nenhum jogador deu "pronto"! Impossível iniciar modo traidor.

## TraitorDeathMatch
traitor-death-match-title = Mata-mata de traidor
traitor-death-match-description = Todos são traidores. Todo mundo quer matar os outros.
traitor-death-match-station-is-too-unsafe-announcement = A estação está muito perigosa para continuar. Você tem um minuto.
traitor-death-match-end-round-description-first-line = Os PDAs se recuperaram depois...
traitor-death-match-end-round-description-entry = PDA de {$originalName}, com {$tcBalance} TC

## TraitorRole

# TraitorRole
traitor-role-greeting = Olá agente
traitor-role-codewords = Suas palavras-chave são: {$codewords}
