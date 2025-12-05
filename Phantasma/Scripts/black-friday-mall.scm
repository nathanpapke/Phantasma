;;;============================================================================
;;; black-friday-mall.scm - Phantasma Milestone 2 Test World
;;; Conversation signature: (lambda (kwd knpc kpc) ...) per Nazghul conv.c
;;;============================================================================

(kern-print "Loading Black Friday Mall...")

;; Constants
(define pclass-grass 1)
(define pclass-wall 9)
(define pclass-water 6)
(define pclass-mountain 8)
(define faction-player 0)
(define faction-ally 1)
(define faction-neutral 2)
(define faction-monster 3)
(define alpha-opaque 255)
(define light-none 0)
(define layer-item 2)
(define gifc-can-get 1)
(define gifc-can-use 2)
(define nil '())

;; Movement modes
(define mmode-walk (kern-mk-mmode 'mmode-walk "Walking" 0))

;; Sprite sets
(define ss-terrain (kern-mk-sprite-set 'ss-terrain 32 32 4 4 0 0 "terrain.png"))
(define ss-characters (kern-mk-sprite-set 'ss-characters 32 32 3 4 0 0 "characters.png"))
(define ss-items (kern-mk-sprite-set 'ss-items 32 32 4 4 0 0 "items.png"))

;; Terrain sprites
(define sprite-grass (kern-mk-sprite 'sprite-grass ss-terrain 1 0 #f 1))
(define sprite-tree (kern-mk-sprite 'sprite-tree ss-terrain 1 1 #f 1))
(define sprite-water (kern-mk-sprite 'sprite-water ss-terrain 1 2 #f 1))
(define sprite-mountain (kern-mk-sprite 'sprite-mountain ss-terrain 1 3 #f 1))
(define sprite-floor sprite-grass)
(define sprite-wall sprite-mountain)

;; Character sprites
(define sprite-player (kern-mk-sprite 'sprite-player ss-characters 1 0 #f 1))
(define sprite-npc (kern-mk-sprite 'sprite-npc ss-characters 1 1 #f 1))
(define sprite-goblin (kern-mk-sprite 'sprite-goblin ss-characters 1 2 #f 1))
(define sprite-merchant sprite-npc)
(define sprite-guard sprite-npc)
(define sprite-shopper sprite-npc)

;; Item sprites (4x4 grid)
(define sprite-gold (kern-mk-sprite 'sprite-gold ss-items 1 0 #f 1))
(define sprite-silver (kern-mk-sprite 'sprite-silver ss-items 1 1 #f 1))
(define sprite-health-potion (kern-mk-sprite 'sprite-health-potion ss-items 1 2 #f 1))
(define sprite-mana-potion (kern-mk-sprite 'sprite-mana-potion ss-items 1 3 #f 1))
(define sprite-rusty-sword (kern-mk-sprite 'sprite-rusty-sword ss-items 2 0 #f 1))
(define sprite-sword (kern-mk-sprite 'sprite-sword ss-items 2 1 #f 1))
(define sprite-shield (kern-mk-sprite 'sprite-shield ss-items 2 2 #f 1))
(define sprite-armor (kern-mk-sprite 'sprite-armor ss-items 2 3 #f 1))
(define sprite-bread (kern-mk-sprite 'sprite-bread ss-items 3 0 #f 1))
(define sprite-meat (kern-mk-sprite 'sprite-meat ss-items 3 1 #f 1))
(define sprite-apple (kern-mk-sprite 'sprite-apple ss-items 3 2 #f 1))
(define sprite-cheese (kern-mk-sprite 'sprite-cheese ss-items 3 3 #f 1))
(define sprite-key (kern-mk-sprite 'sprite-key ss-items 4 0 #f 1))
(define sprite-torch (kern-mk-sprite 'sprite-torch ss-items 4 1 #f 1))
(define sprite-scroll (kern-mk-sprite 'sprite-scroll ss-items 4 2 #f 1))
(define sprite-gem (kern-mk-sprite 'sprite-gem ss-items 4 3 #f 1))

;; Terrain types
(define t-grass (kern-mk-terrain 't-grass "grass" pclass-grass sprite-grass alpha-opaque light-none))
(define t-tree (kern-mk-terrain 't-tree "tree" pclass-wall sprite-tree alpha-opaque light-none))
(define t-water (kern-mk-terrain 't-water "water" pclass-water sprite-water alpha-opaque light-none))
(define t-mountain (kern-mk-terrain 't-mountain "mountain" pclass-mountain sprite-mountain alpha-opaque light-none))
(define t-floor (kern-mk-terrain 't-floor "floor" pclass-grass sprite-floor alpha-opaque light-none))
(define t-wall (kern-mk-terrain 't-wall "wall" pclass-wall sprite-wall alpha-opaque light-none))

;; Item types (kern-mk-obj-type tag name sprite layer capabilities gifc)
(define t-gold (kern-mk-obj-type 't-gold "Gold Coins" sprite-gold layer-item gifc-can-get nil))
(define t-silver (kern-mk-obj-type 't-silver "Silver Coins" sprite-silver layer-item gifc-can-get nil))
(define t-health-potion (kern-mk-obj-type 't-health-potion "Health Potion" sprite-health-potion layer-item (+ gifc-can-get gifc-can-use) nil))
(define t-mana-potion (kern-mk-obj-type 't-mana-potion "Mana Potion" sprite-mana-potion layer-item (+ gifc-can-get gifc-can-use) nil))
(define t-rusty-sword (kern-mk-obj-type 't-rusty-sword "Rusty Sword" sprite-rusty-sword layer-item gifc-can-get nil))
(define t-sword (kern-mk-obj-type 't-sword "Steel Sword" sprite-sword layer-item gifc-can-get nil))
(define t-shield (kern-mk-obj-type 't-shield "Wooden Shield" sprite-shield layer-item gifc-can-get nil))
(define t-bread (kern-mk-obj-type 't-bread "Loaf of Bread" sprite-bread layer-item (+ gifc-can-get gifc-can-use) nil))
(define t-meat (kern-mk-obj-type 't-meat "Cooked Meat" sprite-meat layer-item (+ gifc-can-get gifc-can-use) nil))
(define t-apple (kern-mk-obj-type 't-apple "Fresh Apple" sprite-apple layer-item (+ gifc-can-get gifc-can-use) nil))
(define t-cheese (kern-mk-obj-type 't-cheese "Cheese Wedge" sprite-cheese layer-item (+ gifc-can-get gifc-can-use) nil))
(define t-key (kern-mk-obj-type 't-key "Brass Key" sprite-key layer-item gifc-can-get nil))
(define t-torch (kern-mk-obj-type 't-torch "Torch" sprite-torch layer-item (+ gifc-can-get gifc-can-use) nil))
(define t-scroll (kern-mk-obj-type 't-scroll "Scroll" sprite-scroll layer-item (+ gifc-can-get gifc-can-use) nil))
(define t-gem (kern-mk-obj-type 't-gem "Sparkling Gem" sprite-gem layer-item gifc-can-get nil))

;; Species and occupations
(define sp-human (kern-mk-species 'sp-human "Human" 10 10 10 10 10 mmode-walk 10 5 5 2 nil nil #t nil nil nil 10 '() '()))
(define sp-goblin (kern-mk-species 'sp-goblin "Goblin" 8 6 12 12 8 mmode-walk 6 3 2 1 nil nil #t nil nil nil 5 '() '()))
(define occ-warrior (kern-mk-occ 'occ-warrior "Warrior" 0.0 10 3 0 0 3 2 2 1 5))
(define occ-merchant (kern-mk-occ 'occ-merchant "Merchant" 0.5 0 1 0 0 0 0 0 0 0))
(define occ-guard (kern-mk-occ 'occ-guard "Guard" 0.0 10 3 0 0 2 2 1 1 5))
(define occ-shopper (kern-mk-occ 'occ-shopper "Shopper" 0.0 0 1 0 0 0 0 0 0 0))

;;;============================================================================
;;; CONVERSATIONS - Nazghul signature: (lambda (kwd knpc kpc) ...)
;;; kwd = keyword string (truncated to 4 chars), knpc = NPC, kpc = player
;;;============================================================================

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

(define conv-mystique
  (lambda (kwd knpc kpc)
    (cond
      ((string=? kwd "hail")
        (kern-conv-say knpc "*mysterious fog swirls*")
        (kern-conv-say knpc "I am Madame Mystique...")
        (kern-conv-say knpc "I sense you seek potions of great power..."))
      ((string=? kwd "name")
        (kern-conv-say knpc "I am... Mystique. That is all you need know."))
      ((string=? kwd "job")
        (kern-conv-say knpc "I brew potions and read the stars..."))
      ((string=? kwd "bye")
        (kern-conv-say knpc "The spirits foretold your departure...")
        (kern-conv-end))
      (else
        (kern-conv-say knpc "The mists obscure your meaning...")))))

(define conv-guard
  (lambda (kwd knpc kpc)
    (cond
      ((string=? kwd "hail")
        (kern-conv-say knpc "Move along, citizen.")
        (kern-conv-say knpc "No loitering in the mall corridors."))
      ((string=? kwd "name")
        (kern-conv-say knpc "Officer Jenkins. Mall security."))
      ((string=? kwd "job")
        (kern-conv-say knpc "I keep the peace. No running, no stealing."))
      ((string=? kwd "bye")
        (kern-conv-say knpc "Stay out of trouble.")
        (kern-conv-end))
      (else
        (kern-conv-say knpc "I said move along.")))))

(define conv-shopper1
  (lambda (kwd knpc kpc)
    (cond
      ((string=? kwd "hail")
        (kern-conv-say knpc "Oh my gosh, have you SEEN the deals today?!")
        (kern-conv-say knpc "I've been waiting since MIDNIGHT!"))
      ((string=? kwd "bye")
        (kern-conv-say knpc "Happy shopping!")
        (kern-conv-end))
      (else
        (kern-conv-say knpc "Sorry, can't talk, gotta shop!")))))

(define conv-shopper2
  (lambda (kwd knpc kpc)
    (cond
      ((string=? kwd "hail")
        (kern-conv-say knpc "Excuse me, do you know where the food court is?")
        (kern-conv-say knpc "I'm starving after all this shopping!"))
      ((string=? kwd "bye")
        (kern-conv-say knpc "Thanks anyway!")
        (kern-conv-end))
      (else
        (kern-conv-say knpc "So hungry...")))))

(define conv-shopper3
  (lambda (kwd knpc kpc)
    (cond
      ((string=? kwd "hail")
        (kern-conv-say knpc "*looks stressed*")
        (kern-conv-say knpc "I need to find a gift for my spouse...")
        (kern-conv-say knpc "Any suggestions? Time is running out!"))
      ((string=? kwd "bye")
        (kern-conv-say knpc "Wish me luck!")
        (kern-conv-end))
      (else
        (kern-conv-say knpc "No time to chat!")))))

(define conv-goblin
  (lambda (kwd knpc kpc)
    (cond
      ((string=? kwd "hail")
        (kern-conv-say knpc "Grrr! This MY parking spot!")
        (kern-conv-say knpc "You no take candle! ...er, parking space!"))
      ((string=? kwd "bye")
        (kern-conv-say knpc "Good! Go away!")
        (kern-conv-end))
      (else
        (kern-conv-say knpc "Grrr!")))))

;; Create the mall
(define mall (kern-mk-place 'mall "The Grand Mall of Olde" #f #f 64 64))
(kern-place-set-current mall)

;; Terrain helper
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

;; Build the mall
(fill-area t-grass 0 0 63 63)
(fill-area t-wall 10 10 54 10)
(fill-area t-wall 10 50 54 50)
(fill-area t-wall 10 10 10 50)
(fill-area t-wall 54 10 54 50)
(fill-area t-floor 11 11 53 49)

;; Gilda's shop (NW)
(fill-area t-wall 12 12 20 12)
(fill-area t-wall 12 18 20 18)
(fill-area t-wall 12 12 12 18)
(fill-area t-wall 20 12 20 18)
(fill-area t-floor 13 13 19 17)
(kern-place-set-terrain mall 16 18 t-floor)

;; Bort's shop (NE)
(fill-area t-wall 30 12 40 12)
(fill-area t-wall 30 18 40 18)
(fill-area t-wall 30 12 30 18)
(fill-area t-wall 40 12 40 18)
(fill-area t-floor 31 13 39 17)
(kern-place-set-terrain mall 35 18 t-floor)

;; Mystique's shop (SW)
(fill-area t-wall 12 35 20 35)
(fill-area t-wall 12 42 20 42)
(fill-area t-wall 12 35 12 42)
(fill-area t-wall 20 35 20 42)
(fill-area t-floor 13 36 19 41)
(kern-place-set-terrain mall 16 35 t-floor)

;; Outdoor features
(fill-area t-tree 0 0 3 10)
(fill-area t-tree 60 0 63 10)
(fill-area t-water 30 53 34 56)
(fill-area t-mountain 28 2 36 5)
(kern-place-set-terrain mall 32 5 t-grass)

;; Entrance
(kern-place-set-terrain mall 30 50 t-floor)
(kern-place-set-terrain mall 31 50 t-floor)
(kern-place-set-terrain mall 32 50 t-floor)
(kern-place-set-terrain mall 33 50 t-floor)

;; Characters
(define ch-wanderer
  (kern-mk-char 'ch-wanderer "The Wanderer"
    sp-human occ-warrior sprite-player faction-player
    12 10 11 10 5 5 2 60 0 20 1
    #f nil nil nil nil '() '()))

(define ch-gilda
  (kern-mk-char 'ch-gilda "Gilda the Jeweler"
    sp-human occ-merchant sprite-merchant faction-ally
    8 14 10 5 2 10 3 30 0 40 3
    #f conv-gilda nil nil nil '() '()))

(define ch-bort
  (kern-mk-char 'ch-bort "Bort the Smith"
    sp-human occ-warrior sprite-merchant faction-ally
    16 8 12 15 4 0 0 80 0 0 5
    #f conv-bort nil nil nil '() '()))

(define ch-mystique
  (kern-mk-char 'ch-mystique "Madame Mystique"
    sp-human occ-merchant sprite-merchant faction-ally
    6 18 10 5 2 20 5 25 0 100 7
    #f conv-mystique nil nil nil '() '()))

(define ch-guard
  (kern-mk-char 'ch-guard "Security Guard"
    sp-human occ-guard sprite-guard faction-neutral
    14 8 12 12 3 0 0 50 0 0 3
    #f conv-guard nil nil nil '() '()))

(define ch-shopper1
  (kern-mk-char 'ch-shopper1 "Eager Shopper"
    sp-human occ-shopper sprite-shopper faction-neutral
    10 10 10 5 2 5 2 20 0 10 1
    #f conv-shopper1 nil nil nil '() '()))

(define ch-shopper2
  (kern-mk-char 'ch-shopper2 "Bargain Hunter"
    sp-human occ-shopper sprite-shopper faction-neutral
    10 12 10 5 2 5 2 20 0 10 1
    #f conv-shopper2 nil nil nil '() '()))

(define ch-shopper3
  (kern-mk-char 'ch-shopper3 "Frantic Customer"
    sp-human occ-shopper sprite-shopper faction-neutral
    10 8 14 5 2 5 2 20 0 10 1
    #f conv-shopper3 nil nil nil '() '()))

(define ch-goblin
  (kern-mk-char 'ch-goblin "Parking Lot Goblin"
    sp-goblin occ-warrior sprite-goblin faction-monster
    10 6 14 8 2 2 1 25 0 5 2
    #f conv-goblin nil nil nil '() '()))

;; Place characters
(kern-obj-put-at ch-wanderer (list mall 32 51))
(kern-obj-put-at ch-gilda (list mall 16 15))
(kern-obj-put-at ch-bort (list mall 35 15))
(kern-obj-put-at ch-mystique (list mall 16 39))
(kern-obj-put-at ch-guard (list mall 32 30))
(kern-obj-put-at ch-shopper1 (list mall 25 25))
(kern-obj-put-at ch-shopper2 (list mall 40 40))
(kern-obj-put-at ch-shopper3 (list mall 30 45))
(kern-obj-put-at ch-goblin (list mall 32 7))

;; Place items - ENTRANCE (easy testing)
(kern-obj-put-at (kern-mk-obj t-gold 15) (list mall 31 49))
(kern-obj-put-at (kern-mk-obj t-health-potion 1) (list mall 33 49))

;; GILDA'S JEWELRY
(kern-obj-put-at (kern-mk-obj t-gem 1) (list mall 14 14))
(kern-obj-put-at (kern-mk-obj t-gem 2) (list mall 18 14))
(kern-obj-put-at (kern-mk-obj t-gold 50) (list mall 16 16))
(kern-obj-put-at (kern-mk-obj t-silver 30) (list mall 15 15))

;; BORT'S BLACKSMITH
(kern-obj-put-at (kern-mk-obj t-sword 1) (list mall 32 14))
(kern-obj-put-at (kern-mk-obj t-rusty-sword 2) (list mall 34 14))
(kern-obj-put-at (kern-mk-obj t-shield 1) (list mall 36 14))

;; MYSTIQUE'S POTIONS
(kern-obj-put-at (kern-mk-obj t-health-potion 3) (list mall 14 37))
(kern-obj-put-at (kern-mk-obj t-mana-potion 2) (list mall 18 37))
(kern-obj-put-at (kern-mk-obj t-scroll 1) (list mall 16 38))

;; FOOD COURT
(kern-obj-put-at (kern-mk-obj t-bread 3) (list mall 36 39))
(kern-obj-put-at (kern-mk-obj t-meat 2) (list mall 41 39))
(kern-obj-put-at (kern-mk-obj t-apple 5) (list mall 46 39))
(kern-obj-put-at (kern-mk-obj t-cheese 2) (list mall 36 43))

;; CORRIDOR
(kern-obj-put-at (kern-mk-obj t-gold 10) (list mall 20 25))
(kern-obj-put-at (kern-mk-obj t-torch 2) (list mall 25 30))
(kern-obj-put-at (kern-mk-obj t-key 1) (list mall 40 25))

;; PARKING AREA (near goblin)
(kern-obj-put-at (kern-mk-obj t-rusty-sword 1) (list mall 30 8))
(kern-obj-put-at (kern-mk-obj t-gold 100) (list mall 34 8))
(kern-obj-put-at (kern-mk-obj t-health-potion 2) (list mall 32 9))

;; Player party
(define player-inventory (kern-mk-container nil nil '()))
(kern-mk-player 'player nil "walking" nil 100 50 100 nil nil nil nil player-inventory (list ch-wanderer))

(kern-print "")
(kern-print "Black Friday Mall loaded!")
(kern-print "NPCs: Gilda(NW), Bort(NE), Mystique(SW), Guard, Shoppers, Goblin(north)")
(kern-print "Items: Entrance, shops, food court, corridor, parking area")
(kern-print "Controls: Arrows=Move, T=Talk, G=Get, I=Inventory")
(kern-print "Keywords: hail, name, job, bye")