;; =====================================================================
;; tinyscheme-compat.scm - R5RS/TinyScheme compatibility for IronScheme
;; =====================================================================
;;
;; kern-include is handled at the C# level (intercepted before Eval)
;; DO NOT define kern-include or kern-load here - they are handled in C#

;; =====================================================================
;; Core
;; =====================================================================

(define nil '())

;; =====================================================================
;; TinyScheme Hooks (scripts may reference these)
;; =====================================================================

(define *handlers* '())
(define *error-hook* #f)

;; =====================================================================
;; Passability Constants (used by kern-mk-ptable)
;; =====================================================================

(define norm 1)
(define cant 255)
(define easy 0)

;; =====================================================================
;; List Utilities
;; =====================================================================

(define (foldr proc init lst)
  (if (null? lst)
    init
    (proc (car lst) (foldr proc init (cdr lst)))))

(define (foldl proc init lst)
  (if (null? lst)
    init
    (foldl proc (proc init (car lst)) (cdr lst))))

(define fold foldl)

(define (filter pred lst)
  (cond ((null? lst) '())
    ((pred (car lst))
      (cons (car lst) (filter pred (cdr lst))))
    (else (filter pred (cdr lst)))))

;; =====================================================================
;; Bitwise Operations (TinyScheme names)
;; =====================================================================

(define logand bitwise-and)
(define logior bitwise-ior)
(define logxor bitwise-xor)
(define lognot bitwise-not)
(define ash bitwise-arithmetic-shift)

;; =====================================================================
;; Error Handling
;; =====================================================================

(define (throw . args)
  (apply error args))

(define (defined? sym)
  (guard (ex (else #f))
    (eval sym (interaction-environment))
    #t))