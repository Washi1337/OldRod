Project Old Rod - KoiVM Devirtualisation tool
=============================================
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

Project Old Rod is an automated command-line utility that attempts to disassemble any .NET application protected by the KoiVM virtualiser plugin for ConfuserEx. Additionally, it tries to recompile the VM code back to .NET CIL in an attempt to recover the original code. 

Project Old Rod is released under the GPLv3 license.

Usage:
------

-   **N00b users:**
    Just drag and drop the protected executable in `OldRod` and observe how the majestic Magikarp fixes your code all by itself. Pretty nice huh?

-   **Advanced users:**
    Old Rod has a lot of features! Type the following command in a terminal to get an overview of all available options and flags:
    ```
    OldRod.exe --help
    ```

    Some of the niceties include:
    - `--output-directory`, which sets the output directory (who would have thought!?).
    - `--koi-stream-name`, `--koi-stream-data`, `--entry-type` and `--constants-type`, which help the magestic Magikarp finding the data it needs.
    - `--no-pause` if you don't like pressing a key to continue at the end of it all.

Dependencies
------------
The devirtualiser is powered by the following projects:
- [AsmResolver](https://github.com/Washi1337/AsmResolver): .NET inspection library [![License: LGPL v3](https://img.shields.io/badge/License-LGPL%20v3-blue.svg)](https://www.gnu.org/licenses/lgpl-3.0).
- [Rivers](https://github.com/Washi1337/Rivers): Graph analysis library [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT).

These are submodules located in the `src` directory, so be sure to clone them as well before building the project.


FAQ
===

Is OldRod a deobfuscator?
-------------------------
No. It only disassembles the code and recompiles it. It will not simplify control flow, nor will it decrypt your strings or simplify a lot of arithmetic expressions, or anything like that. For this, other tools exist.


Why did you create this?
------------------------
At the time of writing this, I had noticed quite a few malicious binaries in the wild that used KoiVM to hide their malicious code. Furthermore, especially since the leak of the project, and now that KoiVM has been open-sourced to the public without any restrictions on how to use it, I suspect lots of people would be applying it on their projects as well very soon.

Also I was bored.

Heeeeeelp! it...
-----------------

- ... crashes,
- ... prints errors I don't want to read,
- ... produces corrupted files.

These are features, not bugs. You can turn them off by using:
```
OldRod.exe <input-file> --dont-crash --no-errors --no-output-corruption
```
If that does not work, please consider going to the [issue tracker](https://github.com/Washi1337/OldRod/issues) and file a _detailed_ bug report.

How do I troubleshoot OldRod?
-----------------------------
Old Rod has quite a few diagnostics built-in that might help you out:
- Including `--verbose` will print all debug and full error messages to the standard output. Remember, for large binaries with lots of virtualized code, this can get _very_ verbose quite fast.
- Including `--log-file` will produce a `report.log` in the output directory containing a log that is similar to enabling `--verbose`. You don't need to include `--verbose` to get a verbose output in the log file.
- Including `--dump-il`, `--dump-cil`, `--dump-cfg` and/or `--dump-cfg-all` will create all kinds of dumps of intermediate steps of the devirtualisation process in the output directory.
- Including `--rename-symbols` will rename most (but not all) symbols in the KoiVM runtime library to something more meaningful.
- Including `--salvage` will let the devirtualiser try to recover from errors as much as possible and dump all data it was able to collect. Note that enabling this feature might result in incorrect binaries being produced.
- Including `--only-export 1,2,3` or `--ignore-export 1,2,3` will only include or exclude recompilation of exports 1, 2 and 3 respectively.

Also, be aware this is a **work in progress**, and currently it is in a **very early stage of development**. Sometimes the Magikarp has a tendency to randomly splash around and reach havoc in the file for unknown reasons. Little can be done here other than waiting for the beast to finally mature.

Why is the project so complicated?
----------------------------------
KoiVM is complicated, at least more complicated than the average VM that is out there for .NET. It implements an instruction set that is slightly lower level than the good old CIL we know. A lot can be said about how well it is put together, and about the difficulty of translating some lower level language back to CIL.

Still, the code could've been a lot easier. Instead of actually writing a full recompiler engine with lots of compiler theory put into it, I could've chosen for a more pattern-match based algorithm to revert all the virtual opcodes to CIL opcodes, like a normal devirtualizer project would do. 

It is a concious decision of me to not do this. Hardcoding patterns, comes with the problem that this will not work on forks of the virtualizer. Forks that slightly alter the VM code to something else that results in equivalent behaviour, will completely break a pattern-based devirtualizer. This is why I chose for a more generic approach, that pipelines the input KoiVM code through several phases. The VM code is turned into an abstract syntax tree (AST) and then transformations are performed on the nodes instead. This is a lot more resilient to these kinds of "forks" as it does not really depend on raw structure of the code. As a result, a fork must contain some drastic changes to the virtualizer for OldRod to stop recognizing pattern (At least in theory, let's hope this is true in practice).

Also, I am stubborn and I like compilers.

What's with the name and the Magikarp?
--------------------------------------
In the original release of KoiVM, the plugin description mentions a Magikarp virtualising your code. In the original Pokémon games, the best way to catch a Magikarp is using an old rod. 

...

Honestly, I don't know, I am probably weird...
