cmu-medical-examine-wound-line = [color=red]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } { $wounds } on { POSS-ADJ($target) } { $part }.[/color]
cmu-medical-examine-fracture-line = [color=#dca94c]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } { $fracture } in { POSS-ADJ($target) } { $part }.[/color]
cmu-medical-examine-wounds-line = [color=red]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } wounds: { $parts }.[/color]
cmu-medical-examine-fractures-line = [color=#dca94c]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } fractures: { $parts }.[/color]
cmu-medical-examine-body-part-line = { $part }: { $conditions }.

cmu-medical-examine-wound-size-small = small
cmu-medical-examine-wound-size-deep = deep
cmu-medical-examine-wound-size-deep-visible = moderate
cmu-medical-examine-wound-size-gaping = gaping
cmu-medical-examine-wound-size-gaping-visible = large
cmu-medical-examine-wound-size-massive = massive

cmu-medical-examine-wound-type-burn = burn
cmu-medical-examine-wound-type-wound = wound
cmu-medical-examine-wound-type-surgery = surgical wound
cmu-medical-examine-wound-type-trauma = trauma wound

cmu-medical-examine-wound-treated-prefix = treated
cmu-medical-examine-wound-bleeding-suffix = (bleeding)
cmu-medical-examine-wound-bleeding-active = active bleeding

cmu-medical-examine-wound-visible = { $treated ->
    [true] a treated { $size } { $type }
   *[other] a { $size } { $type }
}

cmu-medical-examine-fracture-hairline = { $stabilized ->
    [true]  a stabilized hairline fracture
   *[other] a hairline fracture
}
cmu-medical-examine-fracture-simple = { $stabilized ->
    [true]  a stabilized broken bone
   *[other] a broken bone
}
cmu-medical-examine-fracture-compound = { $stabilized ->
    [true]  a stabilized compound fracture
   *[other] a compound fracture
}
cmu-medical-examine-fracture-comminuted = { $stabilized ->
    [true]  a stabilized shattered bone
   *[other] a shattered bone
}

cmu-medical-examine-eschar = charred burn tissue

cmu-medical-examine-part-head = Head
cmu-medical-examine-part-torso = Torso
cmu-medical-examine-part-arm-left = Left arm
cmu-medical-examine-part-arm-right = Right arm
cmu-medical-examine-part-hand-left = Left hand
cmu-medical-examine-part-hand-right = Right hand
cmu-medical-examine-part-leg-left = Left leg
cmu-medical-examine-part-leg-right = Right leg
cmu-medical-examine-part-foot-left = Left foot
cmu-medical-examine-part-foot-right = Right foot
cmu-medical-examine-part-severed = SEVERED

cmu-medical-examine-list-and =  and
cmu-medical-examine-list-comma-and = { $list }, and { $last }

cmu-medical-detailed-examine-verb = Inspect injuries
cmu-medical-detailed-examine-verb-message = Take a closer look at their injuries.
cmu-medical-detailed-examine-start = You begin checking { THE($target) } for injuries.
cmu-medical-detailed-examine-none = No obvious injuries found.

cmu-medical-detailed-wound-full = { $size } { $mechanism }

cmu-medical-detailed-treatment-optimal = optimal treatment
cmu-medical-detailed-treatment-adequate = adequate treatment
cmu-medical-detailed-treatment-treated = treated
cmu-medical-detailed-treatment-untreated = untreated

cmu-medical-detailed-cleanup-retained-fragment = retained fragments
cmu-medical-detailed-cleanup-poor-closure = poor closure
cmu-medical-detailed-cleanup-charred-tissue = charred tissue
cmu-medical-detailed-cleanup-crush-debris = crush debris
cmu-medical-detailed-cleanup-dirty-dressing = dirty dressing
cmu-medical-detailed-cleanup-needed = cleanup needed: { $items }

cmu-medical-detailed-hint-remove-shrapnel = remove shrapnel
cmu-medical-detailed-hint-sealing-dressing = sealing dressing
cmu-medical-detailed-hint-burn-gel-dressing = burn gel dressing
cmu-medical-detailed-hint-compression-dressing = compression dressing
cmu-medical-detailed-hint-hemostatic-dressing = hemostatic dressing
cmu-medical-detailed-hint-antiseptic-dressing = antiseptic dressing
cmu-medical-detailed-hint-label = optimal: { $hint }

cmu-medical-detailed-bleed-minor = minor
cmu-medical-detailed-bleed-moderate = moderate
cmu-medical-detailed-bleed-severe = severe
cmu-medical-detailed-bleed-arterial = arterial

cmu-medical-detailed-external-bleeding = external bleeding: { $tier }
cmu-medical-detailed-eschar = burn eschar: charred tissue
cmu-medical-detailed-severed = severed
