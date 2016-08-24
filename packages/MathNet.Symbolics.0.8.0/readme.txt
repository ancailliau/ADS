  __  __       _   _       _   _ ______ _______
 |  \/  |     | | | |     | \ | |  ____|__   __|
 | \  / | __ _| |_| |__   |  \| | |__     | |
 | |\/| |/ _` | __| '_ \  | . ` |  __|    | |
 | |  | | (_| | |_| | | |_| |\  | |____   | |
 |_|  |_|\__,_|\__|_| |_(_)_| \_|______|  |_|

 Math.NET Symbolics - http://symbolics.mathdotnet.com
 Copyright (c) Math.NET - Open Source MIT/X11 License

 Math.NET Symbolics v0.8.0

### 0.8.0 - 2016-01-09
* Simplification: more consistent behavior on infinity and complex infinity
* Expression: new Constant expression leaf nodes (e, pi, I, real/floating-point)
* Expression: merge Positive/NegativeInfinity with Infinity
* Expression: Root, Sqrt, Sinh, Cosh, Tanh, ArcSin, ArcCos, ArcTan
* Functions: Sinh, Cosh, Tanh, ArcSin, ArcCos, ArcTan
* Operators: real, pi, infinity, complexInfinity, negativeInfinity
* Operators: log, root, sqrt, sinh, cosh, tanh, arcsin, arccos, arctan
* Numbers: compare/min/max can also handle the new constants
* Structure: collect, collectIdentifiers, collectNumbers, collectFunctions etc.
* Infix: decimal numbers are now parsed as real constant instead of interpreted as rational
* Infix: unicode symbols for infinity, complex infinity and pi
* Calculus: learnt to differentiate the new functions

### 0.7.1 - 2015-10-03
* Revert FParsec dependency from 1.0.2 back to 1.0.1

### 0.7.0 - 2015-10-03
* Updated package dependencies (no functional changes)
* NuGet package now lists the proper FSharp.Core package

### 0.6.0 - 2015-09-29
* Polynomial: square-free factorization
* Polynomial: commonFactors, coefficientMonomial
* Rational: reduce to cancel common simple factors (part of expand)
* Numbers: integer gcd and lcm routines
* Algebraic: summands, factors, factorsInteger
* Expression: FromIntegerFraction

### 0.5.0 - 2015-07-18
* Infix Parser: interpret decimal notation as exact rational numbers ("0.2" -> "1/5")
* Infix Parser: allow white space after number literal
* Calculus: modified argument order for `taylor` for better currying (breaking!)
* Calculus: new `tangentLine`, `normalLine`
* Calculus: new `differentiateAt` as shortcut for `differentiate >> substitute`

### 0.4.0 - 2014-11-26
* Calculus: add taylor expansion function
* Better Paket compatibility (and NuGet with -ExcludeVersion)
* Use MathNet.Numerics v3.3.0

### 0.3.0 - 2014-09-21
* Use official FSharp.Core 3.1.1 NuGet package, drop alpha suffix
* Now using Paket internally to maintain NuGet dependencies

### 0.2.1-alpha - 2014-09-03
* Package fix to include explicit FSharp.Core reference

### 0.2.0-alpha - 2014-09-02
* First actual release
* Added and improved infix and latex expression printers and infix parsers
* C# compatibility work: more idiomatic when used in C# or other .Net languages

### 0.1.0-alpha - 2014-04-07
* Initial version
