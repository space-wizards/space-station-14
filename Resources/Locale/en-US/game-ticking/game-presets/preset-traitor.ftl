
## Traitor

# Shown at the end of a round of Traitor
traitor-round-end-result = {$traitorCount ->
    [one] There was one traitor.
    *[other] There were {$traitorCount} traitors.
}

# Shown at the end of a round of Traitor
traitor-user-was-a-traitor = [color=gray]{$user}[/color] was a traitor.
traitor-user-was-a-traitor-named = [color=White]{$name}[/color] ([color=gray]{$user}[/color]) was a traitor.
traitor-was-a-traitor-named = [color=White]{$name}[/color] was a traitor.

traitor-user-was-a-traitor-with-objectives = [color=gray]{$user}[/color] was a traitor who had the following objectives:
traitor-user-was-a-traitor-with-objectives-named = [color=White]{$name}[/color] ([color=gray]{$user}[/color]) was a traitor who had the following objectives:
traitor-was-a-traitor-with-objectives-named = [color=White]{$name}[/color] was a traitor who had the following objectives:

preset-traitor-objective-issuer-syndicate = [color=#87cefa]The Syndicate[/color]

# Shown at the end of a round of Traitor
traitor-objective-condition-success = {$condition} | [color={$markupColor}]Success![/color]

# Shown at the end of a round of Traitor
traitor-objective-condition-fail = {$condition} | [color={$markupColor}]Failure![/color] ({$progress}%)

traitor-title = Traitor
traitor-not-enough-ready-players = Not enough players readied up for the game! There were {$readyPlayersCount} players readied up out of {$minimumPlayers} needed.
traitor-no-one-ready = No players readied up! Can't start Traitor.

## TraitorDeathMatch
traitor-death-match-title = Traitor Deathmatch
traitor-death-match-description = Everyone's a traitor. Everyone wants each other dead.
traitor-death-match-station-is-too-unsafe-announcement = The station is too unsafe to continue. You have one minute.
traitor-death-match-end-round-description-first-line = The PDAs recovered afterwards...
traitor-death-match-end-round-description-entry = {$originalName}'s PDA, with {$tcBalance} TC

## TraitorRole

# TraitorRole
traitor-role-name = Syndicate Agent
traitor-role-greeting = Hello Agent
traitor-role-codewords = Your codewords are: {$codewords}