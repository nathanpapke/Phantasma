;;; ============================================================================
;;; task15-combat-test.scm
;;; 
;;; Simple test world for Task 15: Basic Combat
;;;
;;; This file creates:
;;; - Basic terrain types (grass, trees, mountains)
;;; - Test weapons (sword, club, dagger)
;;; - Test armor (leather armor, shield)
;;; - Test species (human, goblin)
;;; - Test occupation (warrior)
;;; - Hero character
;;; - Enemy characters for combat testing
;;; - Simple test arena
;;;
;;; To use: Load this file in your Scheme-enabled Phantasma build
;;; ============================================================================

(println "Loading Task 15 Combat Test World...")

;;; ============================================================================
;;; TERRAIN TYPES
;;; ============================================================================

(println "  Creating terrain types...")

;; Basic grass terrain
(kern-mk-terrain-type
  't_grass           ; tag
  "grass"            ; name
  ".g"               ; pclass (passability class)
  #t                 ; sprite (placeholder - will be loaded from assets)
  )

;; Trees - impassable
(kern-mk-terrain-type
  't_trees
  "trees"
  ".."              ; impassable
  #t
  )

;; Mountains - impassable
(kern-mk-terrain-type
  't_mountains
  "mountains"
  ".."              ; impassable
  #t
  )

;; Deep water - impassable for walking
(kern-mk-terrain-type
  't_deep
  "deep water"
  ".."              ; impassable
  #t
  )

;;; ============================================================================
;;; WEAPON TYPES (ArmsType)
;;; ============================================================================

(println "  Creating weapon types...")

;; Fists - basic unarmed attack
(kern-mk-arms-type
  't_fists          ; tag
  "fists"           ; name
  #f                ; sprite (null for now)
  "0"               ; to-hit dice
  "1d2"             ; damage dice
  "0"               ; armor dice
  "0"               ; to-defend dice
  #x01              ; slot mask (SLOT_WEAPON)
  1                 ; hands
  1                 ; range
  1                 ; required action points
  #f                ; missile type
  #f                ; thrown
  #t                ; ubiquitous ammo (always available)
  0                 ; weight
  #f                ; fire sound
  0                 ; gifc capability
  nil               ; gifc closure
  )

;; Short sword
(kern-mk-arms-type
  't_sword
  "short sword"
  #f
  "+2"              ; to-hit bonus
  "1d6+1"           ; damage
  "0"               ; armor
  "0"               ; defend
  #x01              ; SLOT_WEAPON
  1                 ; 1-handed
  1                 ; melee range
  1                 ; 1 AP
  #f
  #f                ; not thrown
  #t
  10                ; weight
  #f
  0
  nil
  )

;; Long sword - more damage, better to-hit
(kern-mk-arms-type
  't_longsword
  "long sword"
  #f
  "+3"              ; better to-hit
  "1d8+2"           ; more damage
  "0"
  "0"
  #x01
  1
  1
  1
  #f
  #f
  #t
  12
  #f
  0
  nil
  )

;; Dagger - can be thrown
(kern-mk-arms-type
  't_dagger
  "dagger"
  #f
  "+1"
  "1d4"
  "0"
  "0"
  #x01
  1
  1                 ; melee range (throwing would be range 3+)
  1
  #f
  #f                ; not thrown in this simple test
  #t
  2
  #f
  0
  nil
  )

;; Club - simple weapon
(kern-mk-arms-type
  't_club
  "club"
  #f
  "0"               ; no to-hit bonus
  "1d6"
  "0"
  "0"
  #x01
  1
  1
  1
  #f
  #f
  #t
  8
  #f
  0
  nil
  )

;; Great sword - 2-handed, heavy damage
(kern-mk-arms-type
  't_greatsword
  "great sword"
  #f
  "+1"
  "2d6+3"           ; big damage
  "0"
  "0"
  #x01
  2                 ; 2-handed
  1
  2                 ; costs 2 AP
  #f
  #f
  #t
  20
  #f
  0
  nil
  )

;;; ============================================================================
;;; ARMOR TYPES
;;; ============================================================================

(println "  Creating armor types...")

;; Leather armor
(kern-mk-arms-type
  't_leather_armor
  "leather armor"
  #f
  "0"               ; no to-hit
  "0"               ; no damage (it's armor)
  "1d2"             ; armor dice
  "0"               ; defense dice
  #x04              ; SLOT_BODY
  0                 ; no hands
  0                 ; no range
  0                 ; no AP cost
  #f
  #f
  #t
  15
  #f
  0
  nil
  )

;; Chain mail - better armor
(kern-mk-arms-type
  't_chainmail
  "chain mail"
  #f
  "0"
  "0"
  "1d4"             ; better armor
  "+1"              ; defense bonus
  #x04
  0
  0
  0
  #f
  #f
  #t
  30
  #f
  0
  nil
  )

;; Shield
(kern-mk-arms-type
  't_shield
  "shield"
  #f
  "0"
  "0"
  "1d3"             ; armor
  "+2"              ; good defense bonus
  #x02              ; SLOT_SHIELD
  1                 ; occupies one hand
  0
  0
  #f
  #f
  #t
  10
  #f
  0
  nil
  )

;;; ============================================================================
;;; SPECIES
;;; ============================================================================

(println "  Creating species...")

;; Human species
(kern-mk-species
  't_human          ; tag
  "human"           ; name
  #f                ; sprite
  2                 ; strength modifier
  2                 ; intelligence modifier  
  2                 ; dexterity modifier
  0                 ; hp modifier
  4                 ; hp multiplier per level
  0                 ; mp modifier
  2                 ; mp multiplier per level
  10                ; vision radius
  1                 ; speed
  #t                ; visible
  100               ; can-hide difficulty
  #f                ; damage sound
  #f                ; movement sound
  0                 ; stealth
  '(#x01 #x02 #x04) ; slots (weapon, shield, body)
  )

;; Goblin species
(kern-mk-species
  't_goblin
  "goblin"
  #f
  -2                ; weaker
  -2                ; dumber
  2                 ; quick
  0
  3                 ; less HP per level
  0
  1
  8                 ; worse vision
  1
  #t
  80
  #f
  #f
  20                ; fairly stealthy
  '(#x01 #x02 #x04)
  )

;;; ============================================================================
;;; OCCUPATIONS
;;; ============================================================================

(println "  Creating occupations...")

;; Warrior occupation
(kern-mk-occ
  't_warrior        ; tag
  "warrior"         ; name
  0.5               ; magic (not very magical)
  5                 ; hp mod
  5                 ; hp mult
  0                 ; mp mod
  1                 ; mp mult
  3                 ; hit bonus
  2                 ; defend bonus
  2                 ; damage bonus
  1                 ; armor bonus
  50                ; XP value
  )

;; Fighter occupation (for enemies)
(kern-mk-occ
  't_fighter
  "fighter"
  0.25
  3
  4
  0
  0
  2
  1
  1
  0
  30
  )

;;; ============================================================================
;;; CHARACTERS
;;; ============================================================================

(println "  Creating test characters...")

;; Hero character
(kern-mk-char
  't_hero           ; tag
  "Hero"            ; name
  't_human          ; species
  't_warrior        ; occupation
  #f                ; sprite
  0                 ; base faction (player faction)
  15                ; strength
  10                ; intelligence
  12                ; dexterity
  0                 ; hp mod
  6                 ; hp mult
  0                 ; mp mod
  3                 ; mp mult
  100               ; current HP
  0                 ; XP
  50                ; current MP
  1                 ; level
  #f                ; dead?
  nil               ; conversation
  nil               ; schedule
  nil               ; AI
  nil               ; inventory container
  (list t_longsword) ; readied arms
  nil               ; hooks
  nil               ; factions
  )

;; Goblin enemy
(kern-mk-char
  't_goblin_1
  "Goblin"
  't_goblin
  't_fighter
  #f
  1                 ; enemy faction
  8                 ; strength
  6                 ; intelligence
  10                ; dexterity
  0
  3
  0
  1
  30                ; HP
  0
  10
  1
  #f
  nil
  nil
  nil               ; AI (will be added later)
  nil
  (list t_club)     ; armed with club
  nil
  nil
  )

;; Tougher goblin
(kern-mk-char
  't_goblin_tough
  "Goblin Warrior"
  't_goblin
  't_fighter
  #f
  1
  10
  8
  12
  5                 ; more hp mod
  4                 ; more hp mult
  0
  1
  50                ; tougher
  0
  15
  2                 ; level 2
  #f
  nil
  nil
  nil
  nil
  (list t_sword t_shield) ; better equipped
  nil
  nil
  )

;; Orc enemy
(kern-mk-char
  't_orc_1
  "Orc"
  't_goblin          ; reusing goblin species for simplicity
  't_fighter
  #f
  1
  12                 ; stronger
  6
  8
  5
  5
  0
  1
  60                 ; more HP
  0
  20
  2
  #f
  nil
  nil
  nil
  nil
  (list t_sword)
  nil
  nil
  )

;;; ============================================================================
;;; TEST ARENA
;;; ============================================================================

(println "  Creating test arena...")

;; Create a simple 20x20 arena map
(kern-mk-place
  'p_arena          ; tag
  "Combat Arena"    ; name
  nil               ; sprite
  nil               ; wilderness terrain (not wilderness)
  #f                ; wrapping
  #f                ; underground
  #f                ; large-scale (not large scale)
  1                 ; scale
  nil               ; subplace table
  20                ; width
  20                ; height
  #f                ; ambience sound

  ;; Layer list - define the map layout
  (list
    ;; Terrain layer - simple grass floor with mountain border
    (list
      "tttttttttttttttttttt"  ; row 0 - mountains
      "t..................t"  ; row 1
      "t..................t"
      "t..................t"
      "t..................t"
      "t..................t"  ; row 5 - hero will be at (5,5)
      "t..................t"
      "t..................t"
      "t..................t"
      "t..................t"
      "t..................t"  ; row 10
      "t..................t"
      "t..................t"
      "t..................t"
      "t..................t"
      "t..................t"  ; row 15
      "t..................t"
      "t..................t"
      "t..................t"
      "tttttttttttttttttttt"  ; row 19 - mountains
      )
    )
  )

;; Place hero in arena at position (5, 5)
(kern-obj-put-at 't_hero 'p_arena 5 5)

;; Place enemies at various positions for testing
(kern-obj-put-at 't_goblin_1 'p_arena 8 5)      ; East of hero
(kern-obj-put-at 't_goblin_tough 'p_arena 5 8)  ; South of hero
(kern-obj-put-at 't_orc_1 'p_arena 10 10)       ; Southeast corner

;;; ============================================================================
;;; PARTY SETUP
;;; ============================================================================

(println "  Setting up player party...")

;; Create player party and add hero
(kern-mk-party
  'party_player     ; tag
  1                 ; formation (single file)
  )

;; Add hero to player party
(kern-party-add-member 'party_player 't_hero)

;; Set this as the player's party
(kern-set-player-party 'party_player)

;; Set starting location
(kern-set-place 'p_arena)

;;; ============================================================================
;;; TESTING HELPERS
;;; ============================================================================

(println "")
(println "╔════════════════════════════════════════════════════════════╗")
(println "║                                                            ║")
(println "║           TASK 15 COMBAT TEST WORLD LOADED                ║")
(println "║                                                            ║")
(println "╚════════════════════════════════════════════════════════════╝")
(println "")
(println "Test Setup:")
(println "  Location: Combat Arena (20x20)")
(println "  Hero: Level 1 Warrior with Long Sword")
(println "    - Position: (5, 5)")
(println "    - HP: 100")
(println "    - Equipped: Long Sword (1d8+2 damage, +3 to-hit)")
(println "")
(println "  Enemies:")
(println "    - Goblin (5, 8) - Club, 30 HP")
(println "    - Goblin Warrior (8, 5) - Sword + Shield, 50 HP")
(println "    - Orc (10, 10) - Sword, 60 HP")
(println "")
(println "Test Commands:")
(println "  (attack <direction>)  - Attack in a direction")
(println "  (auto-attack)         - Attack nearest enemy")
(println "  (show-stats)          - Show character stats")
(println "")
(println "Example:")
(println "  (attack 'east)        - Attack the Goblin Warrior")
(println "  (attack 'south)       - Attack the Goblin")
(println "")
(println "Ready to test combat!")
(println "")

;;; Helper function to show character stats
(define (show-stats char-tag)
  (let ((char (eval char-tag)))
    (println (kern-obj-get-name char))
    (println "  HP: " (kern-char-get-hp char) "/" (kern-char-get-max-hp char))
    (println "  Level: " (kern-char-get-level char))
    (println "  Position: " (kern-obj-get-loc char))
    ))

;;; Helper to show all character stats
(define (show-all-stats)
  (println "═══ Character Status ═══")
  (show-stats 't_hero)
  (show-stats 't_goblin_1)
  (show-stats 't_goblin_tough)
  (show-stats 't_orc_1)
  )

;;; Export helper functions
(println "Helper functions loaded:")
(println "  (show-stats 't_hero)")
(println "  (show-all-stats)")