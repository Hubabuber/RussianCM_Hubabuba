cmu-gun-safety-verb-on = Поставить на предохранитель

cmu-gun-safety-verb-off = Снять с предохранителя

cmu-gun-safety-on = Вы ставите {THE($gun)} на предохранитель.

cmu-gun-safety-off = Вы снимаете {THE($gun)} с предохранителя.

cmu-gun-safety-blocked = {CAPITALIZE(THE($gun))} на предохранителе!

cmu-gun-safety-examine-on = Предохранитель [color=yellow]включён[/color].

cmu-gun-safety-examine-carried = {CAPITALIZE(POSS-ADJ($user))} {$gun} {$loaded ->
    [yes] [color=red]заряжено[/color]
   *[no] [color=gray]разряжено[/color]
}{$safety ->
    [on] {" "}и стоит на [color=yellow]предохранителе[/color].
    [off] {" "}и снято с [color=orange]предохранителя[/color].
   *[none] .
}

cmu-gun-chamber-loaded-examine = В патроннике [color=yellow]есть патрон[/color].

cmu-gun-chamber-empty-examine = Патронник [color=gray]пуст[/color].
