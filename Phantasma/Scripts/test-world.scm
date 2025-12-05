;;;============================================================================
;;; test-world.scm
;;; 
;;; A simple test world for Phantasma that uses ONLY the kern-* functions
;;; we know are working. This file should load successfully!
;;;
;;; This is the "Black Friday Mall" lite version - terrain only for now.
;;; Characters and items will be added as kern-* functions are implemented.
;;;============================================================================

(kern-print "")
(kern-print "========================================")
(kern-print "  Loading Test World")
(kern-print "  The Grand Mall of Olde (Preview)")
(kern-print "========================================")
(kern-print "")

;;;============================================================================
;;; PASSABILITY CONSTANTS
;;;============================================================================

(define pclass-grass     1)   ; Walkable ground
(define pclass-water     6)   ; Need boat
(define pclass-wall      9)   ; Impassable

;;;============================================================================
;;; FACTION CONSTANTS
;;;============================================================================

(define faction-player   0)   ; Player's team
(define faction-ally     1)   ; Friendly NPCs
(define faction-neutral  2)   ; Non-hostile
(define faction-monster  3)   ; Enemies

;;;============================================================================
;;; SPRITE SETS
;;; Load the sprite sheet images.
;;; 
;;; terrain.png layout (32x32 tiles):
;;;   Row 0: [grass] [tree] [water] [mountain] ...
;;; 
;;; characters.png layout (32x32 tiles):
;;;   Row 0: [player] [npc] [enemy] ...
;;;============================================================================

(kern-print "Loading sprite sets...")

(define ss-terrain
  (kern-mk-sprite-set
    'ss-terrain    ; tag
    32 32          ; tile dimensions
    4 4            ; rows, columns  
    0 0            ; offset
    "terrain.png"))

(define ss-characters
  (kern-mk-sprite-set
    'ss-characters
    32 32
    3 4
    0 0
    "characters.png"))

(kern-print "  OK - Sprite sets loaded")

;;;============================================================================
;;; SPRITES
;;; Create sprites from sprite set tiles.
;;; (kern-mk-sprite tag sprite-set n-frames index wave facings)
;;;============================================================================

(kern-print "Creating sprites...")

;; Terrain sprites (indices in terrain.png)
(define sprite-grass    (kern-mk-sprite 'sprite-grass    ss-terrain 1 0 #f 1))
(define sprite-tree     (kern-mk-sprite 'sprite-tree     ss-terrain 1 1 #f 1))
(define sprite-water    (kern-mk-sprite 'sprite-water    ss-terrain 1 2 #f 1))
(define sprite-mountain (kern-mk-sprite 'sprite-mountain ss-terrain 1 3 #f 1))

;; Character sprites (indices in characters.png)
(define sprite-player   (kern-mk-sprite 'sprite-player   ss-characters 1 0 #f 1))
(define sprite-npc      (kern-mk-sprite 'sprite-npc      ss-characters 1 1 #f 1))
(define sprite-enemy    (kern-mk-sprite 'sprite-enemy    ss-characters 1 2 #f 1))

(kern-print "  OK - Sprites created")

;;;============================================================================
;;; TERRAIN TYPES
;;; (kern-mk-terrain tag name pclass sprite alpha light)
;;;============================================================================

(kern-print "Defining terrain types...")

(define t-grass
  (kern-mk-terrain 't-grass "grass" pclass-grass sprite-grass 255 0))

(define t-tree
  (kern-mk-terrain 't-tree "tree" pclass-wall sprite-tree 255 0))

(define t-water
  (kern-mk-terrain 't-water "water" pclass-water sprite-water 255 0))

(define t-mountain
  (kern-mk-terrain 't-mountain "mountain" pclass-wall sprite-mountain 255 0))

(kern-print "  OK - Terrain types defined")

;;;============================================================================
;;; SPECIES AND OCCUPATIONS
;;; Required to create characters.
;;; Full signatures based on Nazghul's kern.c
;;;============================================================================

(kern-print "Defining species and occupations...")

;; Species definition:
;; (kern-mk-species tag name str int dex spd vr mmode 
;;                  hpmod hpmult mpmod mpmult
;;                  sleep-sprite weapon visible 
;;                  damage-sound walking-sound on-death
;;                  xpval slots spells)

(define sp-human
  (kern-mk-species
    'sp-human        ; tag
    "Human"          ; name
    10               ; str - strength
    10               ; int - intelligence  
    10               ; dex - dexterity
    10               ; spd - speed
    10               ; vr - vision range
    mmode-walk       ; movement mode (from naz.scm)
    10               ; hpmod - base HP contribution
    5                ; hpmult - HP per level
    5                ; mpmod - base MP contribution
    2                ; mpmult - MP per level
    nil              ; sleep-sprite (none for now)
    nil              ; weapon (unarmed)
    #t               ; visible
    nil              ; damage-sound
    nil              ; walking-sound
    nil              ; on-death procedure
    10               ; xpval - XP reward for killing
    '()              ; slots list (equipment slots)
    '()))            ; spells list (innate spells)

;; Occupation definition:
;; (kern-mk-occ tag name magic hpmod hpmult mpmod mpmult hit def dam arm xpval)

(define occ-adventurer
  (kern-mk-occ
    'occ-adventurer  ; tag
    "Adventurer"     ; name
    1.0              ; magic multiplier
    5                ; hpmod - HP bonus from occupation
    2                ; hpmult - additional HP per level
    3                ; mpmod - MP bonus
    1                ; mpmult - additional MP per level
    1                ; hit - to-hit bonus
    0                ; def - defense bonus
    0                ; dam - damage bonus
    0                ; arm - armor bonus
    0))              ; xpval - XP for having this occupation

;; Additional occupations for NPCs
(define occ-merchant
  (kern-mk-occ
    'occ-merchant
    "Merchant"
    0.5              ; low magic
    0 1 0 0          ; hp/mp mods
    0 0 0 0          ; combat mods
    0))

(define occ-guard
  (kern-mk-occ
    'occ-guard
    "Guard"
    0.0              ; no magic
    10 3 0 0         ; more HP
    2 2 1 1          ; better combat
    5))

(kern-print "  OK - Species and occupations defined")

;;;============================================================================
;;; THE PLACE (MAP)
;;; Create a 64x64 tile map.
;;; (kern-mk-place tag name ??? wrapping? width height)
;;;============================================================================

(kern-print "Creating the map (64x64)...")

(define the-mall
  (kern-mk-place 'the-mall "The Grand Mall of Olde" #f #f 64 64))

;; IMPORTANT: Register as THE current place for C# to find
(kern-place-set-current the-mall)

(kern-print "  OK - Map created and registered")

;;;============================================================================
;;; TERRAIN PLACEMENT HELPERS
;;;============================================================================

;; Fill a rectangular area with terrain
(define (fill x1 y1 x2 y2 terrain)
  (let loop-y ((y y1))
    (when (<= y y2)
      (let loop-x ((x x1))
        (when (<= x x2)
          (kern-place-set-terrain the-mall x y terrain)
          (loop-x (+ x 1))))
      (loop-y (+ y 1)))))

;;;============================================================================
;;; BUILD THE MALL
;;;============================================================================

(kern-print "Building the mall...")

;; Step 1: Fill with grass
(kern-print "  Base terrain...")
(fill 0 0 63 63 t-grass)

;; Step 2: Create mall building (mountain walls as placeholder)
(kern-print "  Mall walls...")
;; North wall
(fill 10 10 54 10 t-mountain)
;; South wall  
(fill 10 50 54 50 t-mountain)
;; West wall
(fill 10 10 10 50 t-mountain)
;; East wall
(fill 54 10 54 50 t-mountain)

;; Step 3: Create shops (small enclosed areas)
(kern-print "  Shops...")

;; Shop 1 (northwest)
(fill 12 12 22 12 t-mountain)
(fill 12 20 22 20 t-mountain)
(fill 12 12 12 20 t-mountain)
(fill 22 12 22 20 t-mountain)
;; Door
(kern-place-set-terrain the-mall 17 20 t-grass)

;; Shop 2 (northeast)
(fill 32 12 44 12 t-mountain)
(fill 32 20 44 20 t-mountain)
(fill 32 12 32 20 t-mountain)
(fill 44 12 44 20 t-mountain)
;; Door
(kern-place-set-terrain the-mall 38 20 t-grass)

;; Shop 3 (southwest)
(fill 12 38 22 38 t-mountain)
(fill 12 46 22 46 t-mountain)
(fill 12 38 12 46 t-mountain)
(fill 22 38 22 46 t-mountain)
;; Door
(kern-place-set-terrain the-mall 17 38 t-grass)

;; Step 4: Outdoor features
(kern-print "  Outdoor features...")

;; Entrance plaza (clear area south of mall)
;; Already grass from base fill

;; Fountain (water feature)
(fill 30 53 34 57 t-water)

;; Tree clusters around entrance
(fill 15 52 18 55 t-tree)
(fill 45 52 48 55 t-tree)

;; Forest borders
(fill 0 0 4 8 t-tree)      ; NW
(fill 59 0 63 8 t-tree)    ; NE
(fill 0 58 4 63 t-tree)    ; SW
(fill 59 58 63 63 t-tree)  ; SE

;; Parking dungeon entrance (mountains with gap)
(fill 28 0 36 4 t-mountain)
(kern-place-set-terrain the-mall 32 4 t-grass)  ; Entrance

;; Mall entrance (gap in south wall)
(kern-place-set-terrain the-mall 30 50 t-grass)
(kern-place-set-terrain the-mall 31 50 t-grass)
(kern-place-set-terrain the-mall 32 50 t-grass)
(kern-place-set-terrain the-mall 33 50 t-grass)
(kern-place-set-terrain the-mall 34 50 t-grass)

;; Food court tables (trees as placeholders)
(kern-place-set-terrain the-mall 35 40 t-tree)
(kern-place-set-terrain the-mall 40 40 t-tree)
(kern-place-set-terrain the-mall 45 40 t-tree)

(kern-print "  Mall construction complete!")

;;;============================================================================
;;; PLAYER CHARACTER
;;; The Wanderer - our protagonist!
;;; Created AFTER the map so we can place them on it.
;;;============================================================================

(kern-print "Creating player character...")

;; Create the player character - The Wanderer!
(define ch-wanderer
  (kern-mk-char
    'ch-wanderer           ; tag
    "The Wanderer"         ; name
    sp-human               ; species
    occ-adventurer         ; occupation
    sprite-player          ; sprite
    faction-player         ; base_faction
    12                     ; strength
    10                     ; intelligence
    11                     ; dexterity
    10                     ; hp-mod
    5                      ; hp-mult
    5                      ; mp-mod
    2                      ; mp-mult
    60                     ; starting hp
    0                      ; starting xp
    20                     ; starting mp
    1                      ; starting level
    #f                     ; dead
    nil                    ; conv (conversation)
    nil                    ; sched (schedule)
    nil                    ; ai
    nil                    ; inventory
    '()                    ; readied arms list
    '()))                  ; hooks list

;; Place the player at the mall entrance (just south of the doors).
(kern-obj-put-at ch-wanderer (list the-mall 32 51))

;; Create inventory container for player (required by kern-mk-player).
(define player-inventory (kern-mk-container nil nil '()))

;; Create player party with The Wanderer
(kern-mk-player
  'player                 ; tag
  nil                     ; sprite (use member sprites)
  "walking"               ; movement description
  nil                     ; movement sound
  100                     ; food
  50                      ; gold
  100                     ; turns to next meal
  nil                     ; formation
  nil                     ; campsite
  nil                     ; camp formation
  nil                     ; vehicle
  player-inventory        ; inventory container
  (list ch-wanderer))     ; party members

(kern-print "  ✓ Player 'The Wanderer' created at (32, 51)")

;;;============================================================================
;;; DONE!
;;;============================================================================

(kern-print "")
(kern-print "========================================")
(kern-print "  TEST WORLD LOADED SUCCESSFULLY!")
(kern-print "========================================")
(kern-print "")
(kern-print "Map: 64x64 tiles")
(kern-print "")
(kern-print "Features:")
(kern-print "  [Grass]    - Walkable terrain")
(kern-print "  [Mountain] - Mall walls (impassable)")
(kern-print "  [Tree]     - Forest/tables (impassable)")
(kern-print "  [Water]    - Fountain (impassable)")
(kern-print "")
(kern-print "Layout:")
(kern-print "  North: Parking dungeon entrance")
(kern-print "  Center: Mall building with 3 shops")
(kern-print "  South: Entrance plaza with fountain")
(kern-print "  Corners: Forest areas")
(kern-print "")
(kern-print "Test Controls:")
(kern-print "  Arrow keys / Numpad: Move")
(kern-print "  F5: Quick Save")
(kern-print "  F9: Quick Load")
(kern-print "  G: Get/Pickup (when items exist)")
(kern-print "  T: Talk (when NPCs exist)")
(kern-print "")
(kern-print "Player starts at: (32, 51)")
(kern-print "")