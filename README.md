Project Old Rod - KoiVM Devirtualisation tool
=============================================

Project Old Rod is an automated command-line utility that attempts to disassemble any .NET application protected by the KoiVM virtualiser plugin for ConfuserEx. Additionally, it tries to recompile the VM code back to .NET CIL in an attempt to recover the original code. 

Project Old Rod is released under the GPLv3 license.

Usage:
------

-   *N00b users:*
    Just drag and drop the protected executable in `OldRod` and observe how the majestic Magikarp fixes your code all by itself. Pretty nice huh?

-   *Advanced users:*
    Type the following command in a terminal to get an overview of all available options and flags:
    ```
    OldRod.exe --help
    ```

-   Heeeeeelp! it
    - crashes,
    - prints errors I do not want to read,
    - produces corrupted files.

    These are features, not bugs. You can turn them off by using
    ```
    OldRod.exe <input-file> --dont-crash --no-errors --no-output-corruption
    ```
    If that does not work, please consider going to the [issue tracker](https://github.com/Washi1337/OldRod/issues) and file a _detailed_ bug report.

    Also, be aware this is a *work in progress*, and currently it is in a *very early stage of development*. Sometimes the Magikarp has a tendency to randomly splash around and reach havoc in the file for unknown reasons. Little can be done here other than waiting for the beast to finally mature.

Dependencies
------------
The devirtualiser is powered by the following projects:
- [AsmResolver](https://github.com/Washi1337/AsmResolver): .NET inspection library.
- [Rivers](https://github.com/Washi1337/Rivers): Graph analysis library.

These are submodules located in the `src` directory, so be sure to clone them as well before building the project.

What's with the name and the Magikarp?
--------------------------------------
In the original release of KoiVM, the plugin description mentions a Magikarp virtualising your code. In the original Pokémon games, the best way to catch a Magikarp is using an old rod. 

...

Honestly, I don't know, I am probably weird...
