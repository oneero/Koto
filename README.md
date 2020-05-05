# Koto
A shitty compiler and "toolset" for Koto, a shitty language for controlling funky exploration robots (among other things) in a game that does not exist yet.

# Development
This used to be in development for a Godot game but I got tired of that so now it's Unity-compatible instead. The game is now being developed in Unity but it will not be a part of this repository.

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
Throw it in a Unity project and start poking around. Connect a GUI to the Testbench script to compile and interpret user inputs. Adapt VMGC to tie in your game logic and plug it in the Testbench as well.

# Big up
Huge thanks to [@munificent](https://github.com/munificent) for writing [Crafting Interpreters](https://www.craftinginterpreters.com/). It's an excellent book on this subject and the only thing keeping me sane.
