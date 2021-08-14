
## Traitor

# Shown at the end of a round of Traitor
traitor-round-end-result = {$traitorCount ->
    [one] Был один предатель.
    *[other] Было {$traitorCount} предателей.
}

# Shown at the end of a round of Traitor
traitor-user-was-a-traitor = {$user} был предателем.

# Shown at the end of a round of Traitor
traitor-objective-list-start = и он преследовал следующие цели:

# Shown at the end of a round of Traitor
traitor-objective-condition-success = {$condition} | [color={$markupColor}]Успешно![/color]

# Shown at the end of a round of Traitor
traitor-objective-condition-fail = {$condition} | [color={$markupColor}]Провал![/color] ({$progress}%)

traitor-title = Traitor
traitor-not-enough-ready-players = Недостаточно игроков готовы к игре! Из {$minimumPlayers} необходимых игроков было {$readyPlayersCount}.
traitor-no-one-ready = Нет готовых игроков! Не удалось начать игру за предателя.

## TraitorDeathMatch
traitor-death-match-title = Смертельный бой предателей
traitor-death-match-description = Все - предатели. Все хотят смерти друг друга.
traitor-death-match-station-is-too-unsafe-announcement = Станция слишком опасна для продолжения. У вас есть одна минута.
traitor-death-match-end-round-description-first-line = После этого КПК восстановились...
traitor-death-match-end-round-description-entry = КПК {$originalName}, с {$tcBalance} TC

## TraitorRole

# TraitorRole
traitor-role-name = Агент синдиката
traitor-role-greeting = Здравствуйте, агент
traitor-role-codewords = Ваши кодовые слова: {$codewords}
