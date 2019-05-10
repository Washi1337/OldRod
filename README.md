Project Old Rod - KoiVM Devirtualisation tool
=============================================

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

### Troubleshooting
Old Rod has quite a few diagnostics built-in:
- Including `--verbose` will print all debug and full error messages to the standard output.
- Including `--log-file` will produce a `report.log` in the output directory containing a log that is similar to enabling `--verbose`.
- Including `--dump-il`, `--dump-cil`, `--dump-cfg` and/or `--dump-cfg-all` will create all kinds of dumps of intermediate steps of the devirtualisation process in the output directory.
- Including `--rename-symbols` will rename most (but not all) symbols in the KoiVM runtime library to something more meaningful.
- Including `--salvage` will let the devirtualiser try to recover from errors as much as possible and dump all data it was able to collect. Note that enabling this feature might result in incorrect binaries being produced.
- Including `--only-export 1,2,3` or `--ignore-export 1,2,3` will only include or exclude recompilation of exports 1, 2 and 3 respectively.

Also, be aware this is a **work in progress**, and currently it is in a **very early stage of development**. Sometimes the Magikarp has a tendency to randomly splash around and reach havoc in the file for unknown reasons. Little can be done here other than waiting for the beast to finally mature.

Dependencies
------------
The devirtualiser is powered by the following projects:
- [AsmResolver](https://github.com/Washi1337/AsmResolver): .NET inspection library (LGPLv3 license).
- [Rivers](https://github.com/Washi1337/Rivers): Graph analysis library (MIT license).

These are submodules located in the `src` directory, so be sure to clone them as well before building the project.

What's with the name and the Magikarp?
--------------------------------------
In the original release of KoiVM, the plugin description mentions a Magikarp virtualising your code. In the original Pokémon games, the best way to catch a Magikarp is using an old rod. 

...

Honestly, I don't know, I am probably weird...
