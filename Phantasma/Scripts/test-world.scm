;;; test-world.scm
;;; Properly structured for Nazghul-style sprite loading
;;; Uses kern-mk-sprite-set + kern-mk-sprite pattern

(kern-print "=== Loading test-world.scm ===")

;;; ============================================================================
;;; SPRITE SETS - Load image files and define grid layouts
;;; ============================================================================

(kern-print "Creating sprite sets...")

;; Load terrain.png as a sprite set
;; Syntax: (kern-mk-sprite-set tag width height rows cols offx offy filename)
;; terrain.png has 6 tiles in a horizontal strip (1 row, 6 columns, 32x32 each)
(define ss_terrain
  (kern-mk-sprite-set 'ss_terrain 32 32 1 6 0 0 "terrain.png"))

;; Load characters.png as a sprite set  
;; characters.png has 3 tiles in a horizontal strip (1 row, 3 columns, 32x32 each)
(define ss_characters
  (kern-mk-sprite-set 'ss_characters 32 32 1 3 0 0 "characters.png"))

(kern-print "Sprite sets loaded successfully.")

;;; ============================================================================
;;; SPRITES - Reference tiles from sprite sets by index
;;; ============================================================================

(kern-print "Creating individual sprites...")

;; Syntax: (kern-mk-sprite tag sprite-set n-frames index wave facings)
;; n-frames: 1 for static sprites
;; index: which tile in the sprite-set (0-based)
;; wave: 0 for no wave effect
;; facings: #f for single facing

;; Terrain sprites (from ss_terrain)
(define sprite-grass
  (kern-mk-sprite 'sprite-grass ss_terrain 1 0 0 #f))      ; index 0

(define sprite-tree
  (kern-mk-sprite 'sprite-tree ss_terrain 1 1 0 #f))       ; index 1

(define sprite-water
  (kern-mk-sprite 'sprite-water ss_terrain 1 2 0 #f))      ; index 2

(define sprite-mountain
  (kern-mk-sprite 'sprite-mountain ss_terrain 1 3 0 #f))   ; index 3

(define sprite-stone
  (kern-mk-sprite 'sprite-stone ss_terrain 1 4 0 #f))      ; index 4

(define sprite-dirt
  (kern-mk-sprite 'sprite-dirt ss_terrain 1 5 0 #f))       ; index 5

;; Character sprites (from ss_characters)
(define sprite-player
  (kern-mk-sprite 'sprite-player ss_characters 1 0 0 #f))  ; index 0

(define sprite-npc
  (kern-mk-sprite 'sprite-npc ss_characters 1 1 0 #f))     ; index 1

(define sprite-enemy
  (kern-mk-sprite 'sprite-enemy ss_characters 1 2 0 #f))   ; index 2

(kern-print "Sprites created successfully")

;;; ============================================================================
;;; TERRAIN TYPES - Define terrain behavior
;;; ============================================================================

(kern-print "Creating terrain types...")

;; Syntax: (kern-mk-terrain tag name pclass sprite alpha light)

(define t_grass
  (kern-mk-terrain 't_grass "grass" 0 sprite-grass 0 0))

(define t_tree
  (kern-mk-terrain 't_tree "forest" 255 sprite-tree 255 0))

(define t_water
  (kern-mk-terrain 't_water "water" 255 sprite-water 0 0))

(define t_mountain
  (kern-mk-terrain 't_mountain "mountain" 255 sprite-mountain 255 0))

(define t_stone
  (kern-mk-terrain 't_stone "stone floor" 0 sprite-stone 0 0))

(define t_dirt
  (kern-mk-terrain 't_dirt "dirt" 0 sprite-dirt 0 0))

(kern-print "Terrain types created successfully")

;;; ============================================================================
;;; MAP CREATION
;;; ============================================================================

(kern-print "Creating large test map (128x128)...")

(define test-place
  (kern-mk-place 'test-place "Test World" #f #f 128 128))

(kern-print "Map created, filling with terrain...")

;;; ============================================================================
;;; TERRAIN LAYOUT
;;; ============================================================================

;; Central lake
(kern-print "Creating central lake...")
(kern-place-set-terrain test-place 55 55 t_water)
(kern-place-set-terrain test-place 56 55 t_water)
(kern-place-set-terrain test-place 57 55 t_water)
(kern-place-set-terrain test-place 55 56 t_water)
(kern-place-set-terrain test-place 56 56 t_water)
(kern-place-set-terrain test-place 57 56 t_water)
(kern-place-set-terrain test-place 55 57 t_water)
(kern-place-set-terrain test-place 56 57 t_water)
(kern-place-set-terrain test-place 57 57 t_water)

;; Northern mountains
(kern-print "Creating northern mountains...")
(kern-place-set-terrain test-place 10 5 t_mountain)
(kern-place-set-terrain test-place 20 5 t_mountain)
(kern-place-set-terrain test-place 30 5 t_mountain)
(kern-place-set-terrain test-place 40 5 t_mountain)
(kern-place-set-terrain test-place 50 5 t_mountain)
(kern-place-set-terrain test-place 60 5 t_mountain)
(kern-place-set-terrain test-place 70 5 t_mountain)
(kern-place-set-terrain test-place 80 5 t_mountain)

;; Eastern forest
(kern-print "Creating eastern forest...")
(kern-place-set-terrain test-place 100 30 t_tree)
(kern-place-set-terrain test-place 101 30 t_tree)
(kern-place-set-terrain test-place 102 30 t_tree)
(kern-place-set-terrain test-place 100 31 t_tree)
(kern-place-set-terrain test-place 101 31 t_tree)
(kern-place-set-terrain test-place 102 31 t_tree)
(kern-place-set-terrain test-place 100 32 t_tree)
(kern-place-set-terrain test-place 101 32 t_tree)
(kern-place-set-terrain test-place 102 32 t_tree)

;; Dirt path
(kern-print "Creating dirt path...")
(kern-place-set-terrain test-place 10 10 t_dirt)
(kern-place-set-terrain test-place 11 10 t_dirt)
(kern-place-set-terrain test-place 12 10 t_dirt)
(kern-place-set-terrain test-place 13 10 t_dirt)
(kern-place-set-terrain test-place 14 10 t_dirt)
(kern-place-set-terrain test-place 15 10 t_dirt)
(kern-place-set-terrain test-place 16 10 t_dirt)
(kern-place-set-terrain test-place 17 10 t_dirt)
(kern-place-set-terrain test-place 18 10 t_dirt)
(kern-place-set-terrain test-place 19 10 t_dirt)
(kern-place-set-terrain test-place 20 10 t_dirt)
(kern-place-set-terrain test-place 21 10 t_dirt)
(kern-place-set-terrain test-place 22 10 t_dirt)
(kern-place-set-terrain test-place 23 10 t_dirt)
(kern-place-set-terrain test-place 24 10 t_dirt)
(kern-place-set-terrain test-place 25 10 t_dirt)

;; Stone plaza
(kern-print "Creating stone plaza...")
(kern-place-set-terrain test-place 26 9 t_stone)
(kern-place-set-terrain test-place 27 9 t_stone)
(kern-place-set-terrain test-place 28 9 t_stone)
(kern-place-set-terrain test-place 26 10 t_stone)
(kern-place-set-terrain test-place 27 10 t_stone)
(kern-place-set-terrain test-place 28 10 t_stone)
(kern-place-set-terrain test-place 26 11 t_stone)
(kern-place-set-terrain test-place 27 11 t_stone)
(kern-place-set-terrain test-place 28 11 t_stone)

;; Starting area grass
(kern-print "Placing grass around starting area...")
(kern-place-set-terrain test-place 9 9 t_grass)
(kern-place-set-terrain test-place 10 9 t_grass)
(kern-place-set-terrain test-place 11 9 t_grass)
(kern-place-set-terrain test-place 12 9 t_grass)
(kern-place-set-terrain test-place 9 10 t_grass)
(kern-place-set-terrain test-place 9 11 t_grass)
(kern-place-set-terrain test-place 10 11 t_grass)
(kern-place-set-terrain test-place 11 11 t_grass)
(kern-place-set-terrain test-place 12 11 t_grass)

;;; ============================================================================
;;; TESTING
;;; ============================================================================

(kern-print "Testing map query functions...")
(kern-print "Map width: ")
(kern-print (kern-place-get-width test-place))
(kern-print "Map height: ")
(kern-print (kern-place-get-height test-place))

(kern-print "Testing terrain queries...")
(kern-place-get-terrain test-place 10 10)  ; dirt
(kern-place-get-terrain test-place 55 55)  ; water

;;; ============================================================================
;;; COMPLETION
;;; ============================================================================

(kern-print "")
(kern-print "=== test-world.scm loaded successfully! ===")
(kern-print "Map: 128x128 tiles")
(kern-print "Features: lake, mountains, forest, dirt path, stone plaza")
(kern-print "Ready to play!")