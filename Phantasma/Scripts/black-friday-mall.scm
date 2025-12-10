;;;============================================================================
;;; black-friday-mall.scm - Simplified Test Version
;;; Uses CURRENT Phantasma implementation (simplified signatures)
;;; This is TEMPORARY - will be rewritten with proper Nazghul signatures later
;;;============================================================================

(kern-print "")
(kern-print "========================================")
(kern-print "  BLACK FRIDAY MEDIEVAL SHOPPING MALL")
(kern-print "  (Simplified Version for Testing)")
(kern-print "========================================")
(kern-print "")

;;;============================================================================
;;; LOAD STANDARD DEFINITIONS
;;;============================================================================

(load "naz.scm")

;;;============================================================================
;;; SPRITE SETS AND SPRITES
;;;============================================================================

(kern-print "Loading sprites...")

;; Sprite sets
(define ss-terrain
  (kern-mk-sprite-set 'ss-terrain 32 32 4 4 0 0 "terrain.png"))

(define ss-characters
  (kern-mk-sprite-set 'ss-characters 32 32 3 4 0 0 "characters.png"))

(define ss-items
  (kern-mk-sprite-set 'ss-items 32 32 4 4 0 0 "items.png"))

;; Terrain sprites
(define sprite-grass (kern-mk-sprite 'sprite-grass ss-terrain 1 0 #f 1))
(define sprite-tree (kern-mk-sprite 'sprite-tree ss-terrain 1 1 #f 1))
(define sprite-water (kern-mk-sprite 'sprite-water ss-terrain 1 2 #f 1))
(define sprite-mountain (kern-mk-sprite 'sprite-mountain ss-terrain 1 3 #f 1))

;; Character sprites
(define sprite-player (kern-mk-sprite 'sprite-player ss-characters 1 0 #f 1))
(define sprite-npc (kern-mk-sprite 'sprite-npc ss-characters 1 1 #f 1))
(define sprite-goblin (kern-mk-sprite 'sprite-goblin ss-characters 1 2 #f 1))

;; Item sprites
(define sprite-gold (kern-mk-sprite 'sprite-gold ss-items 1 0 #f 1))
(define sprite-health-potion (kern-mk-sprite 'sprite-health-potion ss-items 1 2 #f 1))
(define sprite-sword (kern-mk-sprite 'sprite-sword ss-items 2 1 #f 1))

(kern-print "  ✓ Sprites loaded")

;;;============================================================================
;;; TERRAIN TYPES
;;;============================================================================

(kern-print "Creating terrain types...")

(define t-grass
  (kern-mk-terrain 't-grass "grass" pclass-grass sprite-grass 255 0))

(define t-tree
  (kern-mk-terrain 't-tree "tree" pclass-wall sprite-tree 255 0))

(define t-water
  (kern-mk-terrain 't-water "water" pclass-water sprite-water 255 0))

(define t-mountain
  (kern-mk-terrain 't-mountain "mountain" pclass-mountain sprite-mountain 255 0))

(define t-floor t-grass)  ; Floor uses grass sprite
(define t-wall t-mountain)  ; Walls use mountain sprite

(kern-print "  ✓ Terrain types created")

;;;============================================================================
;;; ITEM TYPES
;;;============================================================================

(kern-print "Creating item types...")

(define layer-item 7)
(define gifc-can-get 1)
(define gifc-can-use 2)

(define t-gold
  (kern-mk-obj-type 't-gold "Gold Coins" sprite-gold layer-item gifc-can-get nil))

(define t-health-potion
  (kern-mk-obj-type 't-health-potion "Health Potion" sprite-health-potion
    layer-item (+ gifc-can-get gifc-can-use) nil))

(define t-sword
  (kern-mk-obj-type 't-sword "Steel Sword" sprite-sword layer-item gifc-can-get nil))

(kern-print "  ✓ Item types created")

;;;============================================================================
;;; SPECIES AND OCCUPATIONS
;;;============================================================================

(kern-print "Creating species and occupations...")

;; Human species
(define sp-human
  (kern-mk-species
    'sp-human "Human"
    10 10 10 10 10      ; str int dex spd vr
    mmode-walk
    10 5 5 2            ; hpmod hpmult mpmod mpmult
    nil nil #t nil nil nil
    10 '() '()))

;; Goblin species
(define sp-goblin
  (kern-mk-species
    'sp-goblin "Goblin"
    8 6 12 12 8 mmode-walk
    6 3 2 1
    nil nil #t nil nil nil
    5 '() '()))

;; Occupations
(define occ-adventurer
  (kern-mk-occ 'occ-adventurer "Adventurer" 1.0 5 2 3 1 1 0 0 0 0))

(define occ-merchant
  (kern-mk-occ 'occ-merchant "Merchant" 0.5 0 1 0 0 0 0 0 0 0))

(define occ-warrior
  (kern-mk-occ 'occ-warrior "Warrior" 0.0 10 3 0 0 3 2 2 1 5))

(kern-print "  ✓ Species and occupations created")

;;;============================================================================
;;; CONVERSATIONS
;;;============================================================================

(kern-print "Creating conversations...")

(define conv-gilda
  (lambda (kwd knpc kpc)
    (cond
      ((string=? kwd "hail")
        (kern-conv-say knpc "Welcome to Gilda's Fine Jewelry!")
        (kern-conv-say knpc "Everything is 50% off for Black Friday!"))
      ((string=? kwd "name")
        (kern-conv-say knpc "I am Gilda, purveyor of fine gems and jewelry."))
      ((string=? kwd "job")
        (kern-conv-say knpc "I sell the finest jewelry in all the land!"))
      ((string=? kwd "bye")
        (kern-conv-say knpc "Come back soon, dear customer!")
        (kern-conv-end))
      (else
        (kern-conv-say knpc "I'm not sure what you mean.")))))

(define conv-bort
  (lambda (kwd knpc kpc)
    (cond
      ((string=? kwd "hail")
        (kern-conv-say knpc "Greetings! I am Bort the Smith.")
        (kern-conv-say knpc "Need a blade sharpened?"))
      ((string=? kwd "name")
        (kern-conv-say knpc "The name's Bort. Best smith in the mall."))
      ((string=? kwd "job")
        (kern-conv-say knpc "I forge weapons and armor. Finest quality!"))
      ((string=? kwd "bye")
        (kern-conv-say knpc "May your blade stay sharp!")
        (kern-conv-end))
      (else
        (kern-conv-say knpc "Hmm? Speak up, the forge is loud!")))))

(define conv-goblin
  (lambda (kwd knpc kpc)
    (cond
      ((string=? kwd "hail")
        (kern-conv-say knpc "Grrr! This MY parking spot!")
        (kern-conv-say knpc "You no take candle!"))
      ((string=? kwd "bye")
        (kern-conv-say knpc "Good! Go away!")
        (kern-conv-end))
      (else
        (kern-conv-say knpc "Grrr!")))))

(kern-print "  ✓ Conversations created")

;;;============================================================================
;;; THE PLACE (MAP)
;;; TEMPORARY: Using simplified signature until kern-mk-map is implemented
;;;============================================================================

(kern-print "Creating place...")

;; Create 64x64 map using CURRENT simplified signature
(define mall
  (kern-mk-place 'mall "The Grand Mall of Olde" #f #f 64 64))

;; Set as current place
(kern-place-set-current mall)

(kern-print "  ✓ Place created (64x64)")
(kern-print "Building terrain...")

;; Helper to fill rectangular areas
(define (fill-area terrain x1 y1 x2 y2)
  (let loop-y ((y y1))
    (if (<= y y2)
      (begin
        (let loop-x ((x x1))
          (if (<= x x2)
            (begin
              (kern-place-set-terrain mall x y terrain)
              (loop-x (+ x 1)))))
        (loop-y (+ y 1))))))

;; Base terrain - all grass
(fill-area t-grass 0 0 63 63)

;; Mall building outer walls
(fill-area t-wall 10 10 54 10)   ; North
(fill-area t-wall 10 50 54 50)   ; South
(fill-area t-wall 10 10 10 50)   ; West
(fill-area t-wall 54 10 54 50)   ; East

;; Mall interior floor
(fill-area t-floor 11 11 53 49)

;; Gilda's Jewelry (NW shop)
(fill-area t-wall 12 12 20 12)
(fill-area t-wall 12 18 20 18)
(fill-area t-wall 12 12 12 18)
(fill-area t-wall 20 12 20 18)
(fill-area t-floor 13 13 19 17)
(kern-place-set-terrain mall 16 18 t-floor)  ; Door

;; Bort's Blacksmith (NE shop)
(fill-area t-wall 30 12 40 12)
(fill-area t-wall 30 18 40 18)
(fill-area t-wall 30 12 30 18)
(fill-area t-wall 40 12 40 18)
(fill-area t-floor 31 13 39 17)
(kern-place-set-terrain mall 35 18 t-floor)  ; Door

;; Outdoor features
(fill-area t-tree 0 0 3 10)      ; NW trees
(fill-area t-tree 60 0 63 10)    ; NE trees
(fill-area t-water 30 53 34 56)  ; Fountain

;; Parking dungeon entrance
(fill-area t-mountain 28 2 36 5)
(kern-place-set-terrain mall 32 5 t-grass)  ; Entrance gap

;; Mall entrance
(kern-place-set-terrain mall 30 50 t-floor)
(kern-place-set-terrain mall 31 50 t-floor)
(kern-place-set-terrain mall 32 50 t-floor)
(kern-place-set-terrain mall 33 50 t-floor)
(kern-place-set-terrain mall 34 50 t-floor)

(kern-print "  ✓ Terrain built")

;;;============================================================================
;;; CHARACTERS
;;;============================================================================

(kern-print "Creating characters...")

;; The Wanderer (player)
(define ch-wanderer
  (kern-mk-char
    'ch-wanderer "The Wanderer"
    sp-human occ-adventurer sprite-player faction-player
    12 10 11
    10 5 5 2
    60 0 20 1
    #f
    nil nil nil nil
    '() '()))

;; Gilda the Jeweler
(define ch-gilda
  (kern-mk-char
    'ch-gilda "Gilda the Jeweler"
    sp-human occ-merchant sprite-npc faction-ally
    8 14 10
    5 2 10 3
    30 0 40 3
    #f
    conv-gilda nil nil nil
    '() '()))

;; Bort the Smith
(define ch-bort
  (kern-mk-char
    'ch-bort "Bort the Smith"
    sp-human occ-warrior sprite-npc faction-ally
    16 8 12
    15 4 0 0
    80 0 0 5
    #f
    conv-bort nil nil nil
    '() '()))

;; Parking Lot Goblin
(define ch-goblin
  (kern-mk-char
    'ch-goblin "Parking Lot Goblin"
    sp-goblin occ-warrior sprite-goblin faction-monster
    10 6 14
    8 2 2 1
    25 0 5 2
    #f
    conv-goblin nil nil nil
    '() '()))

(kern-print "  ✓ Characters created")

;;;============================================================================
;;; PLACE OBJECTS
;;;============================================================================

(kern-print "Placing characters and items...")

;; Place characters
(kern-obj-put-at ch-wanderer (list mall 32 51))
(kern-obj-put-at ch-gilda (list mall 16 15))
(kern-obj-put-at ch-bort (list mall 35 15))
(kern-obj-put-at ch-goblin (list mall 32 7))

;; Place items
(kern-obj-put-at (kern-mk-obj t-gold 50) (list mall 31 49))
(kern-obj-put-at (kern-mk-obj t-health-potion 3) (list mall 33 49))
(kern-obj-put-at (kern-mk-obj t-sword 1) (list mall 32 14))
(kern-obj-put-at (kern-mk-obj t-gold 100) (list mall 34 8))

(kern-print "  ✓ Objects placed")

;;;============================================================================
;;; PLAYER PARTY
;;;============================================================================

(kern-print "Creating player party...")

;; Create inventory
(define player-inventory
  (kern-mk-container nil nil '()))

;; Create player party
(kern-mk-player
  'player
  nil
  "walking"
  nil
  100 50 100
  nil nil nil nil
  player-inventory
  (list ch-wanderer))

(kern-print "  ✓ Player party created")

;;;============================================================================
;;; DONE
;;;============================================================================

(kern-print "")
(kern-print "========================================")
(kern-print "  BLACK FRIDAY MALL LOADED!")
(kern-print "========================================")
(kern-print "")
(kern-print "Map: 64x64 tiles")
(kern-print "Location: The Grand Mall of Olde")
(kern-print "")
(kern-print "NPCs:")
(kern-print "  • Gilda the Jeweler (NW shop)")
(kern-print "  • Bort the Smith (NE shop)")
(kern-print "  • Parking Lot Goblin (north)")
(kern-print "")
(kern-print "Player starts at: (32, 51)")
(kern-print "")
(kern-print "Controls:")
(kern-print "  Arrow keys: Move")
(kern-print "  T: Talk")
(kern-print "  G: Get items")
(kern-print "  I: Inventory")
(kern-print "")