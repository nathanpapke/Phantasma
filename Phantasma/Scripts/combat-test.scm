;;; ============================================================================
;;; combat-test.scm
;;; 
;;; Combat Test - VERIFIED
;;; ============================================================================

(kern-print "Loading Combat Test World...")

;;; ============================================================================
;;; SPRITES
;;; ============================================================================

(kern-print "  Loading sprite sheets...")

; Create sprite sets using VERIFIED working pattern from Task 11
(define ss_terrain
  (kern-mk-sprite-set 'ss_terrain 32 32 1 6 0 0 "terrain.png"))

(define ss_characters
  (kern-mk-sprite-set 'ss_characters 32 32 1 6 0 0 "characters.png"))

; Create terrain sprites using VERIFIED working pattern
(define sprite-grass
  (kern-mk-sprite 'sprite-grass ss_terrain 1 0 0 #f))

(define sprite-mountain
  (kern-mk-sprite 'sprite-mountain ss_terrain 1 3 0 #f))

; Create character sprites using VERIFIED working pattern  
(define sprite-hero
  (kern-mk-sprite 'sprite-hero ss_characters 1 0 0 #f))

(define sprite-goblin
  (kern-mk-sprite 'sprite-goblin ss_characters 1 2 0 #f))

(define sprite-orc
  (kern-mk-sprite 'sprite-orc ss_characters 1 4 0 #f))

;;; ============================================================================
;;; TERRAIN (4 params)
;;; ============================================================================

(kern-print "  Creating terrain types...")

(define t_grass (kern-mk-terrain-type 't_grass "grass" ".g" sprite-grass))
(define t_mountains (kern-mk-terrain-type 't_mountains "mountains" ".." sprite-mountain))

;;; ============================================================================
;;; WEAPONS (18 params)
;;; ============================================================================

(kern-print "  Creating weapon types...")

(define t_fists
  (kern-mk-arms-type
    't_fists "fists" #f
    "0" "1d2" "0" "0"
    1 1 1 1
    #f #f #t 0
    #f 0 #f))

(define t_sword
  (kern-mk-arms-type
    't_sword "short sword" #f
    "+2" "1d6+1" "0" "0"
    1 1 1 1
    #f #f #t 10
    #f 0 #f))

(define t_longsword
  (kern-mk-arms-type
    't_longsword "long sword" #f
    "+3" "1d8+2" "0" "0"
    1 1 1 1
    #f #f #t 12
    #f 0 #f))

(define t_club
  (kern-mk-arms-type
    't_club "club" #f
    "0" "1d6" "0" "0"
    1 1 1 1
    #f #f #t 8
    #f 0 #f))

;;; ============================================================================
;;; ARMOR (18 params)
;;; ============================================================================

(kern-print "  Creating armor types...")

(define t_leather
  (kern-mk-arms-type
    't_leather "leather armor" #f
    "0" "0" "1d2" "0"
    4 0 0 0
    #f #f #t 15
    #f 0 #f))

(define t_shield
  (kern-mk-arms-type
    't_shield "shield" #f
    "0" "0" "1d3" "+2"
    2 1 0 0
    #f #f #t 10
    #f 0 #f))

;;; ============================================================================
;;; SPECIES (21 params)
;;; ============================================================================

(kern-print "  Creating species...")

(define t_human
  (kern-mk-species
    't_human "human"
    12 10 12              ; str int dex
    10 10                 ; spd vr
    #f                    ; mmode
    0 4 0 2               ; hpmod hpmult mpmod mpmult
    #f #f                 ; sleep-sprite weapon
    #t                    ; visible
    #f #f                 ; damage-sound walking-sound
    #f                    ; on-death
    10                    ; xpval
    (list 1 2 4)          ; slots
    (list)))              ; spells

(define t_goblin
  (kern-mk-species
    't_goblin "goblin"
    8 6 10
    10 8
    #f
    0 3 0 1
    #f #f
    #t
    #f #f
    #f
    5
    (list 1 2 4)
    (list)))

;;; ============================================================================
;;; OCCUPATIONS (12 params)
;;; ============================================================================

(kern-print "  Creating occupations...")

(define t_warrior
  (kern-mk-occ
    't_warrior "warrior"
    0.5                   ; magic
    5 5 0 1               ; hpmod hpmult mpmod mpmult
    3 2 2 1               ; hit def dam arm
    50))                  ; xpval

(define t_fighter
  (kern-mk-occ
    't_fighter "fighter"
    0.25
    3 4 0 0
    2 1 1 0
    30))

;;; ============================================================================
;;; CHARACTERS (24 params)
;;; ============================================================================

(kern-print "  Creating test characters...")

(define t_hero
  (kern-mk-char
    't_hero "Hero"
    t_human t_warrior
    sprite-hero           ; sprite
    0                     ; base-faction
    15 10 12              ; str int dex
    0 6 0 3               ; hpmod hpmult mpmod mpmult
    100 0 50 1            ; hp xp mp lvl
    #f                    ; dead
    #f                    ; conv
    #f                    ; sched
    #f                    ; ai
    #f                    ; inventory
    (list t_longsword)    ; readied
    (list)))              ; hooks (24th param - NO factions!)

(define t_goblin_1
  (kern-mk-char
    't_goblin_1 "Goblin"
    t_goblin t_fighter
    sprite-goblin
    1
    8 6 10
    0 3 0 1
    30 0 10 1
    #f
    #f #f #f #f
    (list t_club)
    (list)))

(define t_goblin_tough
  (kern-mk-char
    't_goblin_tough "Goblin Warrior"
    t_goblin t_fighter
    sprite-goblin
    1
    10 8 12
    5 4 0 1
    50 0 15 2
    #f
    #f #f #f #f
    (list t_sword t_shield)
    (list)))

(define t_orc_1
  (kern-mk-char
    't_orc_1 "Orc"
    t_goblin t_fighter
    sprite-orc
    1
    12 6 8
    5 5 0 1
    60 0 20 2
    #f
    #f #f #f #f
    (list t_sword)
    (list)))

;;; ============================================================================
;;; PLACE (13 params)
;;; ============================================================================

(kern-print "  Creating test arena...")

(define p_arena
  (kern-mk-place
    'p_arena "Combat Arena"
    #f #f                 ; sprite map
    #f #f #f #t           ; wraps underground wild combat
    (list)                ; subplaces
    (list)                ; neighbors
    (list)                ; contents
    (list)                ; hooks
    (list)))              ; entrances

;;; ============================================================================
;;; PLACEMENT
;;; ============================================================================

(kern-print "  Placing characters...")

(kern-obj-put-at t_hero (list p_arena 5 5))
(kern-obj-put-at t_goblin_1 (list p_arena 8 5))
(kern-obj-put-at t_goblin_tough (list p_arena 5 8))
(kern-obj-put-at t_orc_1 (list p_arena 10 10))

;;; ============================================================================
;;; PLAYER PARTY
;;; ============================================================================

(kern-print "  Creating player party...")

; kern-mk-player creates the player party (the party controlled by the player)
; Parameters: tag sprite mv-desc mv-sound food gold ttnm
;             formation campsite camp-form vehicle inventory members
(define party_player
  (kern-mk-player
    'party_player          ; tag
    sprite-hero            ; sprite (for world map representation)
    "walk" #f              ; movement description, sound
    100 50 100             ; food, gold, turns-to-next-meal
    #f #f #f               ; formation, campsite, camp-formation
    #f                     ; vehicle
    #f                     ; inventory container
    (list t_hero)))        ; members - t_hero is player-controlled

;;; ============================================================================
;;; DONE
;;; ============================================================================

(kern-print "")
(kern-print "Combat Test World Loaded!")
(kern-print "Ready to test!")