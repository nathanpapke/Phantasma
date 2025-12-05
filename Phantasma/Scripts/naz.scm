;;;============================================================================
;;; naz.scm - Standard Nazghul/Phantasma Definitions
;;; 
;;; This file contains constants, macros, and utility functions used by
;;; all game session files. It should be loaded at the start of any
;;; session file with: (load "naz.scm")
;;;============================================================================

(kern-print "Loading naz.scm - Standard definitions...")

;;;============================================================================
;;; PASSABILITY CLASSES
;;; These define what types of terrain/obstacles characters can pass through.
;;; The values must match PassabilityTable.cs constants.
;;;============================================================================

(define pclass-none      0)   ; No special passability
(define pclass-grass     1)   ; Normal land terrain
(define pclass-road      2)   ; Roads, paths
(define pclass-forest    3)   ; Trees, forest
(define pclass-hills     4)   ; Hills, rough terrain
(define pclass-shallow   5)   ; Shallow water (wadeable)
(define pclass-water     6)   ; Normal water (need boat)
(define pclass-deep      7)   ; Deep water
(define pclass-mountain  8)   ; Mountains (impassable)
(define pclass-wall      9)   ; Walls, buildings (impassable)
(define pclass-lava     10)   ; Lava (dangerous)
(define pclass-swamp    11)   ; Swamps, marshes
(define pclass-fire     12)   ; Fire fields
(define pclass-ice      13)   ; Ice
(define pclass-air      14)   ; For flying creatures

;; Convenience aliases (matching Nazghul naming)
(define p_none     pclass-none)
(define p_land     pclass-grass)
(define p_grass    pclass-grass)
(define p_road     pclass-road)
(define p_forest   pclass-forest)
(define p_hills    pclass-hills)
(define p_shallow  pclass-shallow)
(define p_shoals   pclass-shallow)
(define p_water    pclass-water)
(define p_deep     pclass-deep)
(define p_mountain pclass-mountain)
(define p_wall     pclass-wall)
(define p_repel    pclass-wall)
(define p_lava     pclass-lava)
(define p_swamp    pclass-swamp)
(define p_fire     pclass-fire)
(define p_ice      pclass-ice)
(define p_air      pclass-air)

;;;============================================================================
;;; FACTIONS
;;; Define faction IDs for diplomacy and AI behavior.
;;; Higher values = more hostile to player.
;;;============================================================================

(define faction-player    0)   ; The player's faction
(define faction-ally      1)   ; Allied NPCs (merchants, friendlies)
(define faction-neutral   2)   ; Neutral NPCs (townfolk, wanderers)
(define faction-monster   3)   ; Hostile monsters
(define faction-demon     4)   ; Very hostile demons/bosses

;; Convenience aliases
(define f_player   faction-player)
(define f_ally     faction-ally)
(define f_neutral  faction-neutral)
(define f_monster  faction-monster)
(define f_demon    faction-demon)

;;;============================================================================
;;; OBJECT LAYERS
;;; Determines rendering order and interaction priority.
;;;============================================================================

(define layer-terrain-feature 0)
(define layer-mechanism       1)
(define layer-item            2)
(define layer-field           3)
(define layer-being           4)
(define layer-container       5)

;;;============================================================================
;;; ALPHA TRANSPARENCY VALUES
;;;============================================================================

(define alpha-transparent   0)
(define alpha-semi-trans  128)
(define alpha-opaque      255)

;;;============================================================================
;;; LIGHT VALUES
;;;============================================================================

(define light-none    0)
(define light-dim     1)
(define light-normal  2)
(define light-bright  3)
(define light-blinding 4)

;;;============================================================================
;;; STATS RANGES
;;;============================================================================

(define stat-min      1)
(define stat-max     30)
(define stat-average 10)

;;;============================================================================
;;; MOVEMENT MODES
;;; Define how characters move through terrain.
;;;============================================================================

;;;============================================================================
;;; MOVEMENT MODES
;;; Created via kern-mk-mmode. Index maps to passability table.
;;;============================================================================

(define mmode-walk  (kern-mk-mmode 'mmode-walk  "Walking"  0))
(define mmode-swim  (kern-mk-mmode 'mmode-swim  "Swimming" 1))
(define mmode-fly   (kern-mk-mmode 'mmode-fly   "Flying"   2))
(define mmode-sail  (kern-mk-mmode 'mmode-sail  "Sailing"  3))
(define mmode-phase (kern-mk-mmode 'mmode-phase "Phasing"  4))
(define mmode-climb (kern-mk-mmode 'mmode-climb "Climbing" 5))

;;;============================================================================
;;; UTILITY MACROS
;;; These make Scheme code more readable.
;;;============================================================================

;; Boolean conversions
(define true #t)
(define false #f)
(define nil '())

;; Create a list of terrain row (for map building)
(define (make-terrain-row terrain count)
  (if (<= count 0)
    '()
    (cons terrain (make-terrain-row terrain (- count 1)))))

;; Fill a rectangular area with terrain
(define (fill-terrain place terrain x1 y1 x2 y2)
  (let loop-y ((y y1))
    (if (<= y y2)
      (begin
        (let loop-x ((x x1))
          (if (<= x x2)
            (begin
              (kern-place-set-terrain place x y terrain)
              (loop-x (+ x 1)))))
        (loop-y (+ y 1))))))

;; Draw a horizontal line of terrain
(define (hline place terrain x1 x2 y)
  (let loop ((x x1))
    (if (<= x x2)
      (begin
        (kern-place-set-terrain place x y terrain)
        (loop (+ x 1))))))

;; Draw a vertical line of terrain
(define (vline place terrain x y1 y2)
  (let loop ((y y1))
    (if (<= y y2)
      (begin
        (kern-place-set-terrain place x y terrain)
        (loop (+ y 1))))))

;; Draw a rectangle outline
(define (rect-outline place terrain x1 y1 x2 y2)
  (hline place terrain x1 x2 y1)  ; Top
  (hline place terrain x1 x2 y2)  ; Bottom
  (vline place terrain x1 y1 y2)  ; Left
  (vline place terrain x2 y1 y2)) ; Right

;; Create a simple room with walls and floor
(define (make-room place wall-terrain floor-terrain x1 y1 x2 y2)
  (fill-terrain place floor-terrain (+ x1 1) (+ y1 1) (- x2 1) (- y2 1))
  (rect-outline place wall-terrain x1 y1 x2 y2))

;;;============================================================================
;;; DICE ROLLING HELPERS
;;;============================================================================

;; Roll dice notation: (roll-dice num sides modifier)
;; Example: (roll-dice 2 6 3) = 2d6+3
(define (roll-dice num sides modifier)
  (+ modifier (kern-dice-roll num sides)))

;;;============================================================================
;;; COORDINATE HELPERS
;;;============================================================================

;; Create a location structure
(define (mk-loc place x y)
  (list place x y))

(define (loc-place loc) (car loc))
(define (loc-x loc) (cadr loc))
(define (loc-y loc) (caddr loc))

;; Calculate distance between two points
(define (distance x1 y1 x2 y2)
  (let ((dx (- x2 x1))
         (dy (- y2 y1)))
    (sqrt (+ (* dx dx) (* dy dy)))))

;; Manhattan distance (for pathfinding)
(define (manhattan-distance x1 y1 x2 y2)
  (+ (abs (- x2 x1)) (abs (- y2 y1))))

;;;============================================================================
;;; DIRECTION CONSTANTS
;;;============================================================================

(define dir-north     0)
(define dir-northeast 1)
(define dir-east      2)
(define dir-southeast 3)
(define dir-south     4)
(define dir-southwest 5)
(define dir-west      6)
(define dir-northwest 7)
(define dir-here      8)

;; Direction deltas
(define dir-dx (vector  0  1  1  1  0 -1 -1 -1  0))
(define dir-dy (vector -1 -1  0  1  1  1  0 -1  0))

(define (direction-x dir) (vector-ref dir-dx dir))
(define (direction-y dir) (vector-ref dir-dy dir))

;;;============================================================================
;;; STATUS CONDITION FLAGS
;;;============================================================================

(define status-normal     0)
(define status-poisoned   1)
(define status-paralyzed  2)
(define status-sleeping   4)
(define status-dead       8)
(define status-invisible 16)
(define status-confused  32)

;;;============================================================================
;;; DAMAGE TYPES
;;;============================================================================

(define damage-physical  0)
(define damage-fire      1)
(define damage-cold      2)
(define damage-lightning 3)
(define damage-poison    4)
(define damage-magical   5)

;;;============================================================================
;;; END OF STANDARD DEFINITIONS
;;;============================================================================

(kern-print "naz.scm loaded successfully.")