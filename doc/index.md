Overview
========

This collection of (mostly unfinished) documents try to explain the inner workings of Project Old Rod. Old Rod is (probably unnecessarily) complex, and I understand that most people probably won't like to sift over my spaghetti code.

In a Nutshell
-------------
The devirtualization process consists of a couple of stages, and are implemented in the `OldRod.Pipeline` sub project:

1. Locate VM types and constants,
2. Parse the injected VM binary stream headers,
3. Locate virtualized methods,
4. Map constants to VM opcodes and their handlers,
5. Disassemble all virtualized methods,
    - Also discover any hidden or non-exported functions,
6. Analyze the VM code,
    - Detect stack layouts,
    - Add any non-exported method to the .NET metadata,
7. Build AST from disassembled VM code,
    - Translate from stack based to a variable based language,
    - Perform optimisations on AST level,
8. Recompile AST to CIL,
    - Convert AST to a "CIL AST",
    - Perform type inference and insert missing type conversions,
    - Serialize CIL AST to normal CIL,
10. Clean up.

Some deeper explanations:
-------------------------
- [The recompiler](Recompiler.md)