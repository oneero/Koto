# Koto
Koto is a programming language, compiler and toolset for controlling funky exploration robots (among other things) in a game that does not exist yet.

This project has been on hiatus for several years.

![Koto controlling a dapper little crab-bot in Unity](https://github.com/oneero/Koto/blob/main/crabbot.png?raw=true)

# Development
The language and toolset was initially developed for Godot, but has since switched to become Unity-compatible instead. 

Current features:
 * Grouping with parentheses
 * Basic arithmetic with +, -, * and /
 * Logical operations: !, >, <, ==, >= and >=
 * Strings and concatenation with + (only string + string for now)
 * Global variables with var
 * Local variables
 * If and else
 * And/or logical operators
 * While and for loops

# How to try?
The Testbench script connects GUI elements and runs user inputs on the VM. The VM has opcodes for interfacing with the VMGC. The VMGC is intended to interface with the rest of the game logic.

# Big up
Huge thanks to [@munificent](https://github.com/munificent) for writing [Crafting Interpreters](https://www.craftinginterpreters.com/). It's an excellent book on this subject and I highly recommend it to everyone!
