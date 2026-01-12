;; =============================================================================
;; Phantasma init.scm - IronScheme compatibility layer for TinyScheme code
;; =============================================================================
;; This file provides TinyScheme compatibility for code originally written
;; for Nazghul (which used TinyScheme). IronScheme is R6RS-compliant and
;; already provides most standard Scheme functions natively.
;;
;; Only TinyScheme-specific functions that are NOT in R6RS are defined here.
;; =============================================================================

;; Enable case-insensitive symbols (TinyScheme default)
;#!fold-case

;; -----------------------------------------------------------------------------
;; CRITICAL: Custom apply to handle CallTargetN closures from C#
;; -----------------------------------------------------------------------------
;; IronScheme's native apply throws "The method or operation is not implemented"
;; when used with CallTargetN closures (C# functions registered via Closure.Create).
;; We override apply to spread arguments explicitly, bypassing the limitation.

;; Save IronScheme's native apply for fallback
(define %native-apply% apply)

;; Helper to flatten apply's variadic arguments into a single list
;; (apply proc a b '(c d)) -> arg-list = (a b c d)
(define (%flatten-apply-args% args)
  (if (null? args)
    '()
    (if (null? (cdr args))
      (if (list? (car args))
        (car args)
        (list (car args)))
      (cons (car args) (%flatten-apply-args% (cdr args))))))

;; =============================================================================
;; Custom apply for C#/Scheme interoperability
;; IronScheme's native apply cannot invoke CallTargetN closures from C#
;; This version manually unpacks arguments to call the procedure directly
;; =============================================================================

(define (interop-apply proc . args)
  (let ((arg-list (%flatten-apply-args% args)))
    (case (length arg-list)
      ((0) (proc))
      ((1) (proc (list-ref arg-list 0)))
      ((2) (proc (list-ref arg-list 0) (list-ref arg-list 1)))
      ((3) (proc (list-ref arg-list 0) (list-ref arg-list 1) (list-ref arg-list 2)))
      ((4) (proc (list-ref arg-list 0) (list-ref arg-list 1) (list-ref arg-list 2) (list-ref arg-list 3)))
      ((5) (proc (list-ref arg-list 0) (list-ref arg-list 1) (list-ref arg-list 2) (list-ref arg-list 3) (list-ref arg-list 4)))
      ((6) (proc (list-ref arg-list 0) (list-ref arg-list 1) (list-ref arg-list 2) (list-ref arg-list 3) (list-ref arg-list 4) (list-ref arg-list 5)))
      ((7) (proc (list-ref arg-list 0) (list-ref arg-list 1) (list-ref arg-list 2) (list-ref arg-list 3) (list-ref arg-list 4) (list-ref arg-list 5) (list-ref arg-list 6)))
      ((8) (proc (list-ref arg-list 0) (list-ref arg-list 1) (list-ref arg-list 2) (list-ref arg-list 3) (list-ref arg-list 4) (list-ref arg-list 5) (list-ref arg-list 6) (list-ref arg-list 7)))
      ((9) (proc (list-ref arg-list 0) (list-ref arg-list 1) (list-ref arg-list 2) (list-ref arg-list 3) (list-ref arg-list 4) (list-ref arg-list 5) (list-ref arg-list 6) (list-ref arg-list 7) (list-ref arg-list 8)))
      ((10) (proc (list-ref arg-list 0) (list-ref arg-list 1) (list-ref arg-list 2) (list-ref arg-list 3) (list-ref arg-list 4) (list-ref arg-list 5) (list-ref arg-list 6) (list-ref arg-list 7) (list-ref arg-list 8) (list-ref arg-list 9)))
      ((11) (proc (list-ref arg-list 0) (list-ref arg-list 1) (list-ref arg-list 2) (list-ref arg-list 3) (list-ref arg-list 4) (list-ref arg-list 5) (list-ref arg-list 6) (list-ref arg-list 7) (list-ref arg-list 8) (list-ref arg-list 9) (list-ref arg-list 10)))
      ((12) (proc (list-ref arg-list 0) (list-ref arg-list 1) (list-ref arg-list 2) (list-ref arg-list 3) (list-ref arg-list 4) (list-ref arg-list 5) (list-ref arg-list 6) (list-ref arg-list 7) (list-ref arg-list 8) (list-ref arg-list 9) (list-ref arg-list 10) (list-ref arg-list 11)))
      ((13) (proc (list-ref arg-list 0) (list-ref arg-list 1) (list-ref arg-list 2) (list-ref arg-list 3) (list-ref arg-list 4) (list-ref arg-list 5) (list-ref arg-list 6) (list-ref arg-list 7) (list-ref arg-list 8) (list-ref arg-list 9) (list-ref arg-list 10) (list-ref arg-list 11) (list-ref arg-list 12)))
      ((14) (proc (list-ref arg-list 0) (list-ref arg-list 1) (list-ref arg-list 2) (list-ref arg-list 3) (list-ref arg-list 4) (list-ref arg-list 5) (list-ref arg-list 6) (list-ref arg-list 7) (list-ref arg-list 8) (list-ref arg-list 9) (list-ref arg-list 10) (list-ref arg-list 11) (list-ref arg-list 12) (list-ref arg-list 13)))
      ((15) (proc (list-ref arg-list 0) (list-ref arg-list 1) (list-ref arg-list 2) (list-ref arg-list 3) (list-ref arg-list 4) (list-ref arg-list 5) (list-ref arg-list 6) (list-ref arg-list 7) (list-ref arg-list 8) (list-ref arg-list 9) (list-ref arg-list 10) (list-ref arg-list 11) (list-ref arg-list 12) (list-ref arg-list 13) (list-ref arg-list 14)))
      (else
        (%native-apply% proc arg-list)))))

;; =============================================================================
;; R6RS syntax-case macro to override apply at compile time
;; This is more powerful than syntax-rules and can override built-ins
;; =============================================================================

(define-syntax apply
  (lambda (x)
    (syntax-case x ()
      ((_ proc args)
        #'(interop-apply proc args)))))

;; -----------------------------------------------------------------------------
;; Custom recursive-map and recursive-for-each
;; These only call themselves, avoiding IronScheme's special handling of 'map'
;; -----------------------------------------------------------------------------

(define (recursive-map proc lst)
  (if (null? lst)
    '()
    (let ((result (proc (car lst))))
      (cons result (recursive-map proc (cdr lst))))))

(define (map proc lst)
  (recursive-map proc lst))

(define (recursive-for-each proc lst)
  (if (null? lst)
    #t
    (begin
      (proc (car lst))
      (recursive-for-each proc (cdr lst)))))

(define (for-each proc lst)
  (recursive-for-each proc lst))

;; -----------------------------------------------------------------------------
;; TinyScheme-specific utilities not in R6RS
;; -----------------------------------------------------------------------------

;; Successor and predecessor
(define (succ x) (+ x 1))
(define (pred x) (- x 1))

;; Not-equal shorthand
(define (<> n1 n2) (not (= n1 n2)))

;; R5RS name for exact->inexact (R6RS uses just 'inexact')
(define (exact->inexact n) (* n 1.0))

;; Atom predicate (not a pair)
(define (atom? x) (not (pair? x)))

;; Last pair of a list
(define (last-pair x)
  (if (pair? (cdr x))
    (last-pair (cdr x))
    x))

;; Alist constructor: (acons key value alist) => ((key . value) . alist)
(define (acons x y z) (cons (cons x y) z))

;; TinyScheme's foldr has reversed argument order from R6RS fold-right
;; TinyScheme: (foldr f init lst) where f is (lambda (acc item) ...)
;; R6RS:       (fold-right f init lst) where f is (lambda (item acc) ...)
(define (foldr f init lst)
  (if (null? lst)
    init
    (foldr f (f init (car lst)) (cdr lst))))

;; -----------------------------------------------------------------------------
;; Stream primitives (for lazy evaluation)
;; -----------------------------------------------------------------------------
(define (head stream) (car stream))
(define (tail stream) (force (cdr stream)))

;; -----------------------------------------------------------------------------
;; TinyScheme macro system compatibility
;; -----------------------------------------------------------------------------
;; Note: TinyScheme uses 'macro' keyword, IronScheme uses 'define-syntax'
;; These macros provide compatibility for common patterns.

;; 'when' and 'unless' - these are in (rnrs control) but we define them
;; here to ensure they're available without explicit import
(define-syntax when
  (syntax-rules ()
    ((when test expr ...)
      (if test (begin expr ...)))))

(define-syntax unless
  (syntax-rules ()
    ((unless test expr ...)
      (if (not test) (begin expr ...)))))

;; -----------------------------------------------------------------------------
;; Exception handling compatibility
;; -----------------------------------------------------------------------------
;; TinyScheme uses catch/throw, IronScheme uses guard/raise
;; We provide catch/throw as wrappers

(define *handlers* '())

(define (push-handler proc)
  (set! *handlers* (cons proc *handlers*)))

(define (pop-handler)
  (let ((h (car *handlers*)))
    (set! *handlers* (cdr *handlers*))
    h))

(define (more-handlers?)
  (pair? *handlers*))

(define (throw . x)
  (if (more-handlers?)
    (apply (pop-handler) x)
    (apply error x)))

;; catch macro using guard
(define-syntax catch
  (syntax-rules ()
    ((catch handler body ...)
      (guard (ex (else handler))
        body ...))))

;; -----------------------------------------------------------------------------
;; Random number generator (TinyScheme compatible)
;; -----------------------------------------------------------------------------
(define *seed* 1)

(define (random-next)
  (let* ((a 16807)
          (m 2147483647)
          (q (quotient m a))
          (r (modulo m a)))
    (set! *seed*
      (- (* a (- *seed* (* (quotient *seed* q) q)))
        (* (quotient *seed* q) r)))
    (if (< *seed* 0)
      (set! *seed* (+ *seed* m)))
    *seed*))

;; -----------------------------------------------------------------------------
;; SRFI-0 cond-expand (feature-based conditional)
;; -----------------------------------------------------------------------------
(define *features* '(srfi-0 ironscheme phantasma))

(define-syntax cond-expand
  (syntax-rules (and or not else)
    ((cond-expand (else body ...))
      (begin body ...))
    ((cond-expand ((and) body ...) more ...)
      (begin body ...))
    ((cond-expand ((and req1 req2 ...) body ...) more ...)
      (cond-expand
        (req1 (cond-expand ((and req2 ...) body ...) more ...))
        more ...))
    ((cond-expand ((or) body ...) more ...)
      (cond-expand more ...))
    ((cond-expand ((or req1 req2 ...) body ...) more ...)
      (cond-expand
        (req1 body ...)
        ((or req2 ...) body ...)
        more ...))
    ((cond-expand ((not req) body ...) more ...)
      (cond-expand
        (req (cond-expand more ...))
        (else body ...)))
    ((cond-expand (feature body ...) more ...)
      (if (memq 'feature *features*)
        (begin body ...)
        (cond-expand more ...)))))
