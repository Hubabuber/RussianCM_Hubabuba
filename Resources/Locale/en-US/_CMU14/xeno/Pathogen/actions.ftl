cmu-xeno-paralyzing-slash-ready = Your next strike will paralyze!
cmu-xeno-paralyzing-slash-cancel = You relax your stance.
cmu-xeno-paralyzing-slash-hit = Your strike seizes {$target}'s muscles!
cmu-xeno-paralyzing-slash-immune = {$target} is a synthetic and immune to the paralyzing effect!

# Spore Sac
cmu-xeno-spore-sac-max = You already have too many spore sacs placed.
cmu-xeno-spore-sac-place-self = You secrete a spore sac.
cmu-xeno-spore-sac-place-others = {$xeno} secretes a spore sac.
cmu-xeno-spore-sac-release = Spore sac quietly releases gas. 

cmu-xeno-spore-cloud-inhale-self = You inhale some weird, musty gas...
cmu-xeno-spore-cloud-inhale-others = {$target} inhales a cloud of spores!

# Direct Spore Infect
cmu-xeno-direct-spore-infect-invalid = That target can't be infected.
cmu-xeno-direct-spore-infect-dead = Your target is already dead.
cmu-xeno-direct-spore-infect-already = Your target is already infected.
cmu-xeno-direct-spore-infect-hit = You force spores into {$target}!
cmu-xeno-direct-spore-infect-blocked = The protective gear blocks the spores!

# Blight Wave
cmu14-xeno-blight-wave-self = You emit a raspy guttural roar!
cmu14-xeno-blight-wave-others = {$xeno} emits a raspy guttural roar!
cmu14-xeno-blight-wave-hit = The roar overwhelms your entire being!

# Cyclone
cmu14-xeno-cyclone-charge = You dig in for a massive strike!

cmu14-xeno-cyclone-charge-others = {$xeno} digs in for a massive strike!
cmu14-xeno-cyclone-spin = You spin in a devastating arc!
cmu14-xeno-cyclone-spin-others = {$xeno} spins in a devastating arc!

# Blight Core / Overmind
cmu14-blight-core-wrong-hive = This core does not respond to you.
cmu14-blight-core-has-overmind = The Confluence already has an Overmind.
cmu14-blight-core-became-overmind = You merge with the Blight Core. You are the Overmind.
cmu14-overmind-strengthened = Your connection to the Confluence deepens. You feel truly powerful.
cmu14-blight-core-vote-started = {$name} is attempting to ascend to Overmind! Vote for who should lead the hive.
cmu14-blight-core-candidate-joined = {$name} has joined the race to become Overmind! (+10s)
cmu14-blight-core-overmind-died = The Overmind has fallen. Approach the Blight Core to begin a new ascension vote.

# Mycotoxin Inject
cmu14-mycotoxin-inject-invalid = That target cannot be injected.
cmu14-mycotoxin-inject-not-dead = Your target must be dead or dying!
cmu14-mycotoxin-inject-self = You skewer {$target} with your tail, injecting mycotoxin!
cmu14-mycotoxin-inject-target = {$xeno} skewers you with its tail!
cmu14-mycotoxin-inject-start-self = You skewer { $target } with your tail and begin pumping mycotoxin into the corpse...
cmu14-mycotoxin-inject-start-target = { $xeno } skewers the body with its tail...
cmu14-mycotoxin-inject-already-infected = Person is already infected.

cmu-pathogen-ui-overmind-needed-label = [bold][color=red]There must be an Overmind for you to gain points![/color][/bold]

cmu-xeno-infected-bursted-back = {CAPITALIZE(SUBJECT($victim))} {CONJUGATE-HAVE($victim)} {POSS-ADJ($victim)} spine ripped out through {POSS-ADJ($victim)} back!
cmu-xeno-infection-burst-now-xeno-back = We rip {POSS-ADJ($victim)} spine out through {THE($victim)}'s back!

# Queen word

cmu-pathogen-words-of-the-overmind-header = The thoughts of the Overmind echo through the Confluence...

# Blight Core accept/vote windows
cmu14-blight-core-accept-title = Overmind Ascension
cmu14-blight-core-accept-body = The Blight Core calls to you. Will you become the Overmind?
cmu14-blight-core-accept-button = Accept
cmu14-blight-core-decline-button = Decline
cmu14-blight-core-seconds-remaining = {$seconds} seconds remaining...
cmu14-blight-core-vote-title = Overmind Ascension Vote
cmu14-blight-core-vote-body = Vote for who should become the Overmind:
cmu14-blight-core-vote-candidate = {$name} — {$votes} {$votes ->
    [one] vote
   *[other] votes
}
cmu14-blight-core-vote-your-vote = (your vote)