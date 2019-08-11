Recompiler
==========

First, how it doesn't work
--------------------------

While KoiVM does not implement a 1:1 mapping between CIL opcode and VM opcode, it still does more or less define a mapping between CIL opcode and groups of VM instructions. Therefore, in theory, pattern matching on the VM code could be done to figure out what the original CIL code was.

For example, the following CIL code
```
ldloc.1
ldc.i4.2
add
```

could potentially in one version of KoiVM be translated to the following VM code:
```
pushr_dword bp  ; push value of variable 1 stored in current stack frame
pushi_dword 1
add_dword
lind_dword

pushi_dword 2   ; push constant 2

add_dword       ; perform addition
```

Pattern matching is effective and an easy way to create a devirtualizer. However, this is not the approach that Old Rod takes. This is because there is a couple of issues with this method:

- There are quite a few CIL opcodes, which means lots of patterns would have to be defined for the devirtualizer to work (which gets tedious very quickly).
- Because there are so many, this will be very error prone.
- This is not scalable. If a fork of KoiVM slightly changes the code to something equivalent (e.g. changing the order of instructions, inserting bogus instructions etc.), the entire devirtualizer will stop working and all patterns would have to be revisited.
- Lots of type information is lost when the CIL code is translated to the VM code. Pattern matching will only help you to some extend to recover this.

The IL-AST
----------

While disassembling the VM code, the devirtualizer keeps track of control flow and data flow. By paying attention to how many values each instruction pushes onto- and pops from the stack, the devirtualizer figures out which instructions depend on each other, essentially constructing not only a control flow graph (CFG), but also a data flow graph (DFG).

After it has done so, the devirtualizer builds an intermediate representation in the form of an abstract syntax tree (AST) from these two data structures. This is a representation of a custom intermediate language (IL) that aims to completely remove the notion of the stack and registers from the code, and replaces them with variables instead.

Essentially, this translates the VM code to a bunch of assignments:
```
tmp1 = pushr_dword(bp)
tmp2 = pushi_dword(1)
tmp3 = add_dword(tmp1, tmp2)
tmp4 = lind_dword(tmp3)

tmp5 = pushi_dword(2)

tmp6 = add_dword(tmp4, tmp5)
```

These are then inlined in one of the optimisation steps where possible:
```
tmp6 = add(lind_dword(add_dword(pushr_dword(bp), pushi_dword(1))), pushi_dword(2))
```

or in AST form:

```
                       tmp6 = 
                          |
                      add_dword
                          |
                .---------+---------.
                |                   |
           lind_dword         pushi_dword 2
                |
            add_dword
                |
        .-------+-------.
        |               |
  pushr_dword bp   pushi_dword 1
```


The great benefit of this approach is that the resulting tree is not dependent anymore on the raw physical code sequence. Any instructions that would be inserted by some fork, or any shuffling in order of instructions would be removed automatically, making the virtualizer a lot more generic. Furthermore, a tree-like structure is easy to process by using various traversal algorithms and/or visitor patterns. Pattern matching is also much more reliable if we perform it on the AST instead of the raw code for the same reason as previously mentioned. Finally, lots of compiler theory is based on ASTs. Writing a devirtualizer can therefore leverage from this research to recompile and optimise the code.

Optimisations on the IL-AST
------------------------------------------------
While Old Rod does not define patterns for every CIL instruction, it does use pattern matching on some of the IL-AST structures to do certain optimisations. For example, in the left subtree of the resulting IL-AST we obtained, we see a very common construct for loading a variable from the current stack frame. We optimise these away to improve some readability of the code and attempt to reduce the number of variables introduced.

```             
                :
           lind_dword                   
                |                                      :
            add_dword               =>          pushr_dword var1
                |
        .-------+-------.
        |               |
  pushr_dword bp   pushi_dword 1
```

The CIL-AST
-----------

After the IL AST has been made, it is translated to a CIL AST:
```
                       stloc 0
                          |
                         add
                          |
                .---------+---------.
                |                   |
             ldloc 1            ldc.i4 2
```
or in expression form:
```
stloc(0, add(ldloc(1), ldc.i4(2)))
```

In this step, we also reintroduce the .NET typing system. We look at all uses of each variable and guess the corresponding variable type based on its context, and add any missing type casts in the code. Since KoiVM destroys all information about typing, this is a necessary step.

For example, in the tree above we can infer that variable 1 is probably a 32 bit integral type (`int` or `uint`), as it is used in an `add_dword` expression.

Generating the final CIL code
------------------------------

Finally. by performing a depth-first traversal on the resulting CIL-AST, we can obtain the corresponding CIL code:

```
ldloc 1
ldc.i4 2
add
stloc 0
```