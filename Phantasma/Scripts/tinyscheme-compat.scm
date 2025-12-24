;; TinyScheme Compatibility Layer for IronScheme
;; =============================================
;; Provides stubs for TinyScheme-specific features not in R6RS.
;; This file should be loaded FIRST, before any game scripts.
;;
;; DO NOT add (load ...) or (kern-include ...) calls to this file!
;; This is a pure compatibility layer with no dependencies.

;; -----------------------------------------------------------------
;; nil/NIL - TinyScheme's empty list
;; -----------------------------------------------------------------
(define nil '())
(define NIL '())

;; -----------------------------------------------------------------
;; t/f - TinyScheme's booleans (some scripts use these)
;; -----------------------------------------------------------------
(define t #t)
(define f #f)

;; -----------------------------------------------------------------
;; gc-verbose - TinyScheme garbage collector control
;; No-op in IronScheme (let .NET handle GC)
;; -----------------------------------------------------------------
(define (gc-verbose . args) #f)

;; -----------------------------------------------------------------
;; gc - Force garbage collection
;; No-op in IronScheme
;; -----------------------------------------------------------------
(define (gc) #f)

;; -----------------------------------------------------------------
;; gensym - Generate unique symbols
;; Used by some macro implementations
;; -----------------------------------------------------------------
(define gensym
  (let ((counter 0))
    (lambda args
      (set! counter (+ counter 1))
      (string->symbol
        (string-append
          (if (null? args) "g" (symbol->string (car args)))
          (number->string counter))))))

;; -----------------------------------------------------------------
;; call/cc - Common alias for call-with-current-continuation
;; -----------------------------------------------------------------
(define call/cc call-with-current-continuation)

;; -----------------------------------------------------------------
;; print - TinyScheme's print (display + newline)
;; -----------------------------------------------------------------
(define (print . args)
  (for-each display args)
  (newline))

;; -----------------------------------------------------------------
;; atom? - TinyScheme predicate (not a pair)
;; -----------------------------------------------------------------
(define (atom? x)
  (not (pair? x)))

;; -----------------------------------------------------------------
;; TinyScheme Macro Stubs
;; -----------------------------------------------------------------
;; TinyScheme's define-macro and macro are fundamentally incompatible
;; with R6RS syntax-case. These stubs prevent load errors but the
;; actual macros won't work.

;; define-macro - Stub that creates a function instead of a real macro
(define-syntax define-macro
  (syntax-rules ()
    ((_ (name . args) body ...)
      (define (name . args) #f))
    ((_ name expander)
      (define name (lambda args #f)))))

;; macro - Another TinyScheme macro form (silently ignored)
(define-syntax macro
  (syntax-rules ()
    ((_ name . body)
      (begin))))

;; -----------------------------------------------------------------
;; End of compatibility layer
;; -----------------------------------------------------------------