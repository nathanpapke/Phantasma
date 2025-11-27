;; test-npc.scm
;; Simple test NPC with keyword-based conversation

;; Create a test guard NPC
(define test-guard-conv
  (lambda (keyword npc pc)
    (cond
      ;; Initial greeting
      ((equal? keyword "hail")
        (kern-conv-say npc "Greetings, traveler!"))

      ;; Name keyword
      ((equal? keyword "name")
        (kern-conv-say npc "I am Guardsman Bob."))

      ;; Job keyword
      ((equal? keyword "job")
        (kern-conv-say npc "I guard this place from monsters."))

      ;; Help keyword
      ((equal? keyword "help")
        (kern-conv-say npc "Try asking about NAME, JOB, or BYE."))

      ;; Bye - end conversation
      ((equal? keyword "bye")
        (kern-conv-say npc "Farewell!")
        (kern-conv-end))

      ;; Default response
      (else
        (kern-conv-say npc "I don't understand that keyword.")))))

;; Create the NPC character using sprite from test-world.scm
;; Assumes test-world.scm has already loaded and defined sprite-npc
(define guardsman-bob
  (kern-mk-char
    'ch_guardsman_bob ; tag
    "Guardsman Bob"   ; name
    #f                ; species (not implemented yet)
    #f                ; occupation (not implemented yet)
    sprite-npc        ; sprite (second sprite from ss_characters)
    0                 ; base faction
    12                ; strength
    10                ; intelligence
    14                ; dexterity
    20                ; hp mod
    1                 ; hp mult
    10                ; mp mod
    1                 ; mp mult
    50                ; hp
    0                 ; xp
    20                ; mp
    1                 ; level
    #f                ; dead (false)
    test-guard-conv   ; conversation
    #f                ; schedule
    #f                ; ai
    #f))              ; inventory

;; Place the NPC 2 tiles east of player
;; Assumes player is at (64, 64) as defined in test-world.scm
(kern-obj-put-at guardsman-bob test-world 66 64)

(kern-print "test-npc.scm loaded - created Guardsman Bob at (66, 64)")