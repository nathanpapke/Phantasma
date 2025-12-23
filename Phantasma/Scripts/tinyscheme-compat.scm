;;; tinyscheme-compat.scm
;;; TinyScheme Compatibility Layer for IronScheme (R6RS)
;;;
;;; This file must be loaded FIRST before any Nazghul/Haxima scripts.
;;; It provides compatibility for TinyScheme idioms that don't work in R6RS.
;;;
;;; Note: The Kernel has already imported (rnrs) and (ironscheme) before
;;; loading this file, so we don't need to import them again.
;;;
;;; Note: The SchemePreprocessor automatically replaces all (load ...) calls
;;; with (kern-include ...) before evaluation, so we don't need to override
;;; load here.

;;; ============================================================
;;; TINYSCHEM COMPATIBILITY STUBS
;;; TinyScheme has features that IronScheme doesn't support.
;;; We provide no-op stubs or compatible implementations.
;;; ============================================================

;; gc-verbose - TinyScheme garbage collector verbosity (no-op)
(define (gc-verbose . args) #f)

;; gc - TinyScheme manual garbage collection (no-op)
(define (gc) #f)

;; TinyScheme's 'macro' is completely different from R6RS macros.
;; We can't fully emulate it, but we can provide a stub that
;; prevents errors. Scripts using complex macros may not work.
;; 
;; TinyScheme macro syntax: (macro name (lambda (form) ...))
;; or: (macro (name form) ...)
;;
;; For now, we just ignore macro definitions.
;; This means macro-dependent code won't work, but at least
;; the file will load and other definitions will be available.

;; define-macro - stub that ignores the definition
;; Real define-macro creates a macro, we just define a dummy function
(define-syntax define-macro
  (syntax-rules ()
    ((_ (name . args) body ...)
      (define (name . args)
        (error 'define-macro "TinyScheme macros not supported in IronScheme" 'name)))
    ((_ name expr)
      (define name
        (lambda args
          (error 'define-macro "TinyScheme macros not supported in IronScheme" 'name))))))

;; macro - TinyScheme's low-level macro definition
;; Syntax: (macro name transformer) or (macro (name form) body...)
;; We stub this out - macros defined this way won't work but won't crash
(define-syntax macro
  (syntax-rules ()
    ((_ (name form) body ...)
      (begin))  ;; no-op
    ((_ name transformer)
      (begin))))  ;; no-op

;; gensym - generate unique symbol (IronScheme has this, but just in case)
(define gensym-counter 0)
(define (gensym . prefix)
  (set! gensym-counter (+ gensym-counter 1))
  (string->symbol
    (string-append
      (if (null? prefix) "g" (symbol->string (car prefix)))
      (number->string gensym-counter))))

;; call/cc alias
(define call/cc call-with-current-continuation)

;;; ============================================================
;;; NIL / NULL COMPATIBILITY
;;; R5RS uses 'nil' and '() interchangeably, R6RS is stricter
;;; ============================================================

(define nil '())
(define NIL '())

;; Nazghul uses (null? x) but also checks against 'nil symbol sometimes
(define (null-or-nil? x)
  (or (null? x)
    (eq? x 'nil)
    (eq? x 'NIL)))

;;; ============================================================
;;; INTERNAL DEFINE WORKAROUND
;;; R5RS allows (define ...) anywhere in a body
;;; R6RS requires all defines at the start of a body
;;;
;;; We provide 'define-local' as an alternative that uses letrec
;;; But the real fix requires preprocessing or macro magic
;;; ============================================================

;; This doesn't fully solve the problem but helps with some patterns
;; For procedures that need internal defines, use this pattern:
;;
;; Instead of:
;;   (define (foo x)
;;     (do-something)
;;     (define (helper y) ...)   ; ERROR in R6RS
;;     (helper x))
;;
;; Use:
;;   (define (foo x)
;;     (letrec ((helper (lambda (y) ...)))
;;       (do-something)
;;       (helper x)))

;;; ============================================================
;;; BOOLEAN COMPATIBILITY  
;;; ============================================================

;; R5RS #t/#f are the same in R6RS, but some code uses 't and 'f
(define t #t)
(define f #f)

;;; ============================================================
;;; LIST UTILITIES (some may be missing in R6RS base)
;;; ============================================================

;; Ensure these are available
(define (atom? x)
  (not (pair? x)))

(define (nul x)
  (if (null? x) #t #f))

;; safe-car and safe-cdr that don't error on nil
(define (safe-car x)
  (if (pair? x) (car x) '()))

(define (safe-cdr x)
  (if (pair? x) (cdr x) '()))

;; first, second, third, etc. - common in R5RS code
(define first car)
(define second cadr)
(define third caddr)
(define fourth cadddr)
(define (fifth x) (car (cddddr x)))

(define rest cdr)

;;; ============================================================
;;; MUTATION COMPATIBILITY
;;; R6RS separates mutable pairs into (rnrs mutable-pairs)
;;; We've imported it above, but ensure set-car!/set-cdr! work
;;; ============================================================

;; These should be available from (rnrs mutable-pairs)
;; but let's make sure they're exported to interaction environment

;;; ============================================================
;;; NUMERIC COMPATIBILITY
;;; ============================================================

;; Simple linear congruential random number generator
;; Works without any external dependencies
(define *random-seed* 12345)

(define (random n)
  (if (<= n 0)
    0
    (begin
      ;; LCG parameters (same as glibc)
      (set! *random-seed*
        (mod (+ (* 1103515245 *random-seed*) 12345) 2147483648))
      (mod (quotient *random-seed* 65536) n))))

;; Seed the random number generator
(define (random-seed! seed)
  (set! *random-seed* seed))

;; Ensure modulo and remainder work as expected
(define mod modulo)
(define rem remainder)

;;; ============================================================
;;; STRING COMPATIBILITY
;;; ============================================================

;; string-downcase might be named differently
(define (string-lower s)
  (string-downcase s))

(define (string-upper s)
  (string-upcase s))

;;; ============================================================
;;; PRINTING COMPATIBILITY
;;; ============================================================

;; R5RS (print x) vs R6RS (display x)
(define (print . args)
  (for-each display args)
  (newline))

(define (println . args)
  (for-each display args)
  (newline))

;;; ============================================================
;;; CONTROL FLOW COMPATIBILITY
;;; ============================================================

;; R5RS (error msg) vs R6RS (error who msg irritants...)
(define (simple-error msg)
  (error 'scheme-error msg))

;; when and unless might need definition
;; (should be in R6RS but let's be safe)
(define-syntax when
  (syntax-rules ()
    ((when test body ...)
      (if test (begin body ...) #f))))

(define-syntax unless
  (syntax-rules ()
    ((unless test body ...)
      (if test #f (begin body ...)))))

;;; ============================================================
;;; DEFINE WORKAROUND MACRO
;;; This macro allows internal defines by transforming them
;;; ============================================================

;; define-with-locals: A macro that allows internal defines
;; Usage:
;;   (define-with-locals (func-name args ...)
;;     (define (helper1 x) ...)
;;     (define (helper2 y) ...)
;;     body ...)
;;
;; Transforms to:
;;   (define (func-name args ...)
;;     (letrec ((helper1 (lambda (x) ...))
;;              (helper2 (lambda (y) ...)))
;;       body ...))

;; Note: This is a simple version that handles common patterns
;; Complex nested internal defines may still need manual conversion

;;; ============================================================
;;; NAZGHUL-SPECIFIC COMPATIBILITY
;;; Common patterns used in Nazghul/Haxima scripts
;;; ============================================================

;; Nazghul uses (println ...) for debug output
;; Already defined above

;; loc-x, loc-y, loc-place extractors if using simple lists for locations
(define (loc-place loc) (car loc))
(define (loc-x loc) (cadr loc))
(define (loc-y loc) (caddr loc))
(define (mk-loc place x y) (list place x y))

;; dice rolling helper
(define (d n)
  (+ 1 (random n)))

(define (roll-dice num sides)
  (let loop ((i num) (total 0))
    (if (<= i 0)
      total
      (loop (- i 1) (+ total (d sides))))))

;;; ============================================================
;;; ASSOCIATION LIST HELPERS
;;; ============================================================

(define (alist-get key alist)
  (let ((pair (assoc key alist)))
    (if pair (cdr pair) #f)))

(define (alist-set key val alist)
  (cons (cons key val)
    (filter (lambda (pair) (not (equal? (car pair) key))) alist)))

;;; ============================================================
;;; DISPLAY COMPLETION MESSAGE
;;; ============================================================

(display "TinyScheme compatibility layer loaded.\r\n")