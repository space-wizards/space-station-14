parse-minutes-fail = Não foi possível analisar '{$minutes}' como minutos
parse-session-fail = Sessão não encontrada para '{$username}'

## Role Timer Commands

# - playtime_addoverall
cmd-playtime_addoverall-desc = Adiciona os minutos especificados para o tempo geral de jogo de um jogador
cmd-playtime_addoverall-help = Uso: {$command} <user name> <minutes>
cmd-playtime_addoverall-succeed = Aumentado o tempo de jogo geral de {$username} para {TOSTRING($time, "0")}
cmd-playtime_addoverall-arg-user = <user name>
cmd-playtime_addoverall-arg-minutes = <minutes>
cmd-playtime_addoverall-error-args = Experados exatamente dois argumentos

# - playtime_addrole
cmd-playtime_addrole-desc = Adiciona os minutos especificados para o tempo de jogo de uma função de um jogador
cmd-playtime_addrole-help = Uso: {$command} <user name> <role> <minutes>
cmd-playtime_addrole-succeed = Aumentado o tempo de jogo de {$username} / \'{$role}\' para {TOSTRING($time, "0")}
cmd-playtime_addrole-arg-user = <user name>
cmd-playtime_addrole-arg-role = <role>
cmd-playtime_addrole-arg-minutes = <minutes>
cmd-playtime_addrole-error-args = Experados exatamente três argumentos

# - playtime_getoverall
cmd-playtime_getoverall-desc = Mostra o tempo de jogo geral, em minutos, de um jogador
cmd-playtime_getoverall-help = Uso: {$command} <user name>
cmd-playtime_getoverall-success = O tempo de jogo geral de {$username} é {TOSTRING($time, "0")} minutos
cmd-playtime_getoverall-arg-user = <user name>
cmd-playtime_getoverall-error-args = Experado exatamente um argumento

# - GetRoleTimer
cmd-playtime_getrole-desc = Mostra o tempo de jogo de uma ou todas funções, em minutos, de um jogador
cmd-playtime_getrole-help = Uso: {$command} <user name> [role]
cmd-playtime_getrole-no = Nenhum tempo de função encontrado
cmd-playtime_getrole-role = Função: {$role}, Tempo de Jogo: {$time}
cmd-playtime_getrole-overall = Tempo de jogo geral é {$time}
cmd-playtime_getrole-succeed = Tempo de jogo de {$username} é: {TOSTRING($time, "0")}
cmd-playtime_getrole-arg-user = <user name>
cmd-playtime_getrole-arg-role = <role|'Overall'>
cmd-playtime_getrole-error-args = Experados exatamente um ou dois argumentos

# - playtime_save
cmd-playtime_save-desc = Salva o tempo de jogo do jogador para o DB
cmd-playtime_save-help = Uso: {$command} <user name>
cmd-playtime_save-succeed = Tempo de jogo de {$username} salvo.
cmd-playtime_save-arg-user = <user name>
cmd-playtime_save-error-args = Experado exatamente um argumento

## 'playtime_flush' command'

cmd-playtime_flush-desc = Flush active trackers to stored in playtime tracking.
cmd-playtime_flush-help = Usage: {$command} [user name]
    This causes a flush to the internal storage only, it does not flush to DB immediately.
    If a user is provided, only that user is flushed.

cmd-playtime_flush-error-args = Expected zero or one arguments
cmd-playtime_flush-arg-user = [user name]
