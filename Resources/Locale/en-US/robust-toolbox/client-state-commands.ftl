# Loc strings for various entity state & client-side PVS related commands

cmd-reset-ent-help = Использование: { $command } <Entity UID>
cmd-reset-ent-desc = Сбрасывает сущность до последнего полученного от сервера состояния. Это также сбросит сущности, которые были удалены в null-space.

cmd-reset-all-ents-help = Использование: { $command }
cmd-reset-all-ents-desc = Сбрасывает все сущности до последнего полученного от сервера состояния. Это затрагивает только сущности, которые не были удалены в null-space.

cmd-detach-ent-help = Использование: { $command } <Entity UID>
cmd-detach-ent-desc = Удаляет сущность в null-space, как если бы он покинул зону действия PVS.

cmd-local-delete-help = Использование: { $command } <Entity UID>
cmd-local-delete-desc = Удаляет сущность. В отличие от обычной команды delete, эта команда работает на стороне клиента (CLIENT-SIDE). Если сущность не является клиентской, это, скорее всего, приведёт к ошибкам.

cmd-full-state-reset-help = Использование: { $command }
cmd-full-state-reset-desc = Сбрасывает всю информацию о состоянии сущности и запрашивает полное состояние у сервера.
