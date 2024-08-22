parse-minutes-fail = No se pudo analizar '{$minutes}' como minutos
parse-session-fail = No se encontró la sesión para '{$username}'

## Comandos del Temporizador de Tiempo de Juego

# - playtime_addoverall
cmd-playtime_addoverall-desc = Agrega los minutos especificados al tiempo de juego general de un jugador
cmd-playtime_addoverall-help = Uso: {$command} <user name> <minutes>
cmd-playtime_addoverall-succeed = Aumentó el tiempo general para {$username} a {TOSTRING($time, "dddd\\:hh\\:mm")}
cmd-playtime_addoverall-arg-user = <user name>
cmd-playtime_addoverall-arg-minutes = <minutes>
cmd-playtime_addoverall-error-args = Se esperaban exactamente dos argumentos

# - playtime_addrole
cmd-playtime_addrole-desc = Agrega los minutos especificados al tiempo de juego por rol de un jugador
cmd-playtime_addrole-help = Uso: {$command} <user name> <role> <minutes>
cmd-playtime_addrole-succeed = Aumentó el tiempo de juego por rol para {$username} / \'{$role}\' a {TOSTRING($time, "dddd\\:hh\\:mm")}
cmd-playtime_addrole-arg-user = <user name>
cmd-playtime_addrole-arg-role = <role>
cmd-playtime_addrole-arg-minutes = <minutes>
cmd-playtime_addrole-error-args = Se esperaban exactamente tres argumentos

# - playtime_getoverall
cmd-playtime_getoverall-desc = Obtiene los minutos especificados para el tiempo de juego general de un jugador
cmd-playtime_getoverall-help = Uso: {$command} <user name>
cmd-playtime_getoverall-success = El tiempo general para {$username} es {TOSTRING($time, "dddd\\:hh\\:mm")}.
cmd-playtime_getoverall-arg-user = <user name>
cmd-playtime_getoverall-error-args = Se esperaba exactamente un argumento

# - GetRoleTimer
cmd-playtime_getrole-desc = Obtiene todos o uno de los temporizadores de rol de un jugador
cmd-playtime_getrole-help = Uso: {$command} <user name> [role]
cmd-playtime_getrole-no = No se encontraron temporizadores de rol
cmd-playtime_getrole-role = Rol: {$role}, Tiempo de juego: {$time}
cmd-playtime_getrole-overall = El tiempo de juego general es {$time}
cmd-playtime_getrole-succeed = El tiempo de juego para {$username} es: {TOSTRING($time, "dddd\\:hh\\:mm")}.
cmd-playtime_getrole-arg-user = <user name>
cmd-playtime_getrole-arg-role = <role|'Overall'>
cmd-playtime_getrole-error-args = Se esperaban exactamente uno o dos argumentos

# - playtime_save
cmd-playtime_save-desc = Guarda los tiempos de juego del jugador en la base de datos
cmd-playtime_save-help = Uso: {$command} <user name>
cmd-playtime_save-succeed = Tiempo de juego guardado para {$username}
cmd-playtime_save-arg-user = <user name>
cmd-playtime_save-error-args = Se esperaba exactamente un argumento

## Comando 'playtime_flush'

cmd-playtime_flush-desc = Vacía los rastreadores activos para almacenarlos en el seguimiento del tiempo de juego.
cmd-playtime_flush-help = Uso: {$command} [user name]
    Esto causa un vaciado solo en el almacenamiento interno, no se vacía en la base de datos de inmediato.
    Si se proporciona un usuario, solo ese usuario se vacía.

cmd-playtime_flush-error-args = Se esperaban cero o un argumento
cmd-playtime_flush-arg-user = [user name]
