comp-kitchen-spike-deny-collect = { CAPITALIZE($this) } уже чем-то занят, сначала закончите срезать мясо!
comp-kitchen-spike-begin-hook-self-other = { CAPITALIZE(THE($victim)) } begins dragging { REFLEXIVE($victim) } onto { THE($hook) }!
comp-kitchen-spike-begin-hook-other-self = You begin dragging { CAPITALIZE(THE($victim)) } onto { THE($hook) }!
comp-kitchen-spike-begin-hook-other = { CAPITALIZE(THE($user)) } begins dragging { CAPITALIZE(THE($victim)) } onto { THE($hook) }!a
comp-kitchen-spike-hook-self = You threw yourself on { THE($hook) }!
comp-kitchen-spike-hook-self-other = { CAPITALIZE(THE($victim)) } threw { REFLEXIVE($victim) } on { THE($hook) }!
comp-kitchen-spike-hook-other-self = You threw { CAPITALIZE(THE($victim)) } on { THE($hook) }!
comp-kitchen-spike-hook-other = { CAPITALIZE(THE($user)) } threw { CAPITALIZE(THE($victim)) } on { THE($hook) }!
comp-kitchen-spike-begin-unhook-self = You begin dragging yourself off { THE($hook) }!
comp-kitchen-spike-begin-unhook-self-other = { CAPITALIZE(THE($victim)) } begins dragging { REFLEXIVE($victim) } off { THE($hook) }!
comp-kitchen-spike-begin-unhook-other-self = You begin dragging { CAPITALIZE(THE($victim)) } off { THE($hook) }!
comp-kitchen-spike-begin-unhook-other = { CAPITALIZE(THE($user)) } begins dragging { CAPITALIZE(THE($victim)) } off { THE($hook) }!
comp-kitchen-spike-unhook-self = You got yourself off { THE($hook) }!
comp-kitchen-spike-unhook-self-other = { CAPITALIZE(THE($victim)) } got { REFLEXIVE($victim) } off { THE($hook) }!
comp-kitchen-spike-unhook-other-self = You got { CAPITALIZE(THE($victim)) } off { THE($hook) }!
comp-kitchen-spike-unhook-other = { CAPITALIZE(THE($user)) } got { CAPITALIZE(THE($victim)) } off { THE($hook) }!
comp-kitchen-spike-begin-butcher-self = You begin butchering { THE($victim) }!
comp-kitchen-spike-begin-butcher = { CAPITALIZE(THE($user)) } begins to butcher { THE($victim) }!
comp-kitchen-spike-butcher-self = You butchered { THE($victim) }!
comp-kitchen-spike-butcher = { CAPITALIZE(THE($user)) } butchered { THE($victim) }!
comp-kitchen-spike-unhook-verb = Unhook
comp-kitchen-spike-hooked = [color=red]{ CAPITALIZE(THE($victim)) } is on this spike![/color]
comp-kitchen-spike-deny-butcher = { CAPITALIZE($victim) } не может быть разделан на { $this }.
comp-kitchen-spike-victim-examine = [color=orange]{ CAPITALIZE(SUBJECT($target)) } looks quite lean.[/color]
comp-kitchen-spike-deny-butcher-knife = { CAPITALIZE($victim) } не может быть разделан на { $this }, используйте нож для разделки.
comp-kitchen-spike-deny-not-dead =
    { CAPITALIZE($victim) } не может быть разделан. { CAPITALIZE(SUBJECT($victim)) } { GENDER($victim) ->
        [male] ещё жив
        [female] ещё жива
        [epicene] ещё живы
       *[neuter] ещё живо
    }!
comp-kitchen-spike-begin-hook-victim = { CAPITALIZE($user) } начинает насаживать вас на { $this }!
comp-kitchen-spike-begin-hook-self = Вы начинаете насаживать себя на { $this }!
comp-kitchen-spike-kill = { CAPITALIZE($user) } насаживает { $victim } на мясной крюк, тем самым убивая { SUBJECT($victim) }!
comp-kitchen-spike-suicide-other = { CAPITALIZE($victim) } бросается на мясной крюк!
comp-kitchen-spike-suicide-self = Вы бросаетесь на мясной крюк!
comp-kitchen-spike-knife-needed = Вам нужен нож для этого.
comp-kitchen-spike-remove-meat = Вы срезаете немного мяса с { $victim }.
comp-kitchen-spike-remove-meat-last = Вы срезаете последний кусок мяса с { $victim }!
comp-kitchen-spike-meat-name = мясо { $victim }
