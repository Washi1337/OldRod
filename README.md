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

Is Old Rod a deobfuscator?
-------------------------
No. It only disassembles the code and recompiles it. It will not simplify control flow, nor will it decrypt your strings, simplify arithmetic expressions, rename all symbols, decrypt resources, or anything like that. For this, other tools exist.


Why did you create / release this?
----------------------------------
At the time of writing this, I had noticed quite a few malicious binaries in the wild that used KoiVM to hide their malicious code. Furthermore, especially since the leak of the project, and now that KoiVM has been open-sourced to the public without any restrictions on how to use it, I suspect more people applying it to their "products" as well very soon.

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
If that does not work, please consider going to the [issue tracker](https://github.com/Washi1337/OldRod/issues) and file a _detailed_ bug report, taking the following into account:
- Be aware I do this project in my little free time.
- Because of this, when filing a report it is important to narrow down the issue as much as possible to your ability.
    - Issues simply stating "it doesn't work" will be ignored.
- Respect original authors of copyrighted software. **Don't upload copyrighted executables** protected by KoiVM. These issues will be **deleted** immediately.
- Look at the troubleshooting tips in the readme.

Also, be aware this is a **work in progress**. Sometimes the Magikarp has a tendency to randomly splash around and reach havoc in the file for unknown reasons. Little can be done here other than waiting for the beast to finally mature.

How do I troubleshoot Old Rod?
-----------------------------
Old Rod has quite a few diagnostics built-in that might help you out:
- Including `--verbose` will print all debug and full error messages to the standard output. Remember, for large binaries with lots of virtualized code, this can get _very_ verbose quite fast.
- Including `--log-file` will produce a `report.log` in the output directory containing a log that is similar to enabling `--verbose`. You don't need to include `--verbose` to get a verbose output in the log file.
- Including `--dump-il`, `--dump-cil`, `--dump-cfg` and/or `--dump-cfg-all` will create all kinds of dumps of intermediate steps of the devirtualisation process in the output directory.
- Including `--rename-symbols` will rename most (but not all) symbols in the KoiVM runtime library to something more meaningful.
- Including `--salvage` will let the devirtualiser try to recover from errors as much as possible and dump all data it was able to collect. Note that enabling this feature might result in incorrect binaries being produced.
- Including `--only-export 1,2,3` or `--ignore-export 1,2,3` will only include or exclude recompilation of exports 1, 2 and 3 respectively.

Why is Old Rod slower than other deobfuscators or devirtualizers?
-----------------------------------------------------------------
Because the project is complicated, and I am probably not the best coder or reverse engineer.

Why is the project so complicated?
----------------------------------
Because KoiVM is complicated, at least more complicated than the average VM that is out there for .NET. It implements an instruction set that is slightly lower level than the good old CIL we know. A lot can be said about how well it is put together, and about the difficulty of translating some lower level language back to a higher level language such as CIL. 

Old Rod is a recompiler, and does not use a lot of pattern matching for mapping sequences of VM code to CIL code. Rather, it works with abstract syntax trees (AST) and a bit of compiler theory to do the translations and transformations.

Couldn't you just use pattern matching for every CIL instruction like normal people?
------------------------------------------------------------------------------------
Sure.

However, it is a concious decision of me to not do this. Hardcoding patterns comes with the problem that this will not work on forks of the virtualizer. Forks that slightly alter the VM code to something else that results in equivalent behaviour, will completely break a pattern-based devirtualizer. 

This is why I chose for a more generic approach, that treats the input KoiVM code as an input source file for a compiler. Working on ASTs and performing transformations on the nodes of this AST is a lot more resilient to these kinds of changes, as it does not have a strong relation with the physical structure and layout of the code. As a result, a fork must contain some drastic changes to the virtualizer for Old Rod to stop from recompiling the code (At least in theory, fingers crossed).

Also, I am stubborn, I don't like to write countless of patterns, and I like writing compilers.

What is the OldRod.Core.CodeGen namespace that is injected?
-----------------------------------------------------------
Not all instructions are always perfectly translated to CIL, and still require some of the original features of KoiVM's virtual machine (most notably, the flags register as the CLR does not have one). For this, the code generator might inject some code to emulate the behaviour of the feature. This is put into this namespace.

What's with the name and the Magikarp?
--------------------------------------
In the original release of KoiVM, the plugin description mentions a Magikarp virtualising your code. In the original Pokémon games, the best way to catch a Magikarp is using an old rod. 

...

Honestly, I don't know, I am probably weird...
