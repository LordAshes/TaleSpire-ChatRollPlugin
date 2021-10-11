# Chat Roll Plugin

This unofficial TaleSpire plugin allows performing rolls from the chat menu. Supports public rolls as
well as private rolls sent only to the GM.
 
## Change Log

1.0.0: Initial release

## Install

Use R2ModMan or similar installer to install this plugin.

For full functionality installing the Chat Whisper Plugin is recommended but not required.
   
## Usage

Open the player or character chat. Use one of the following syntaxes to activate the roller functionality:

### Roll: /r formula

This rolls the specific formula and outputs the results in the Chat for everyone to see. Includes the roll
request, the roll request with all the dice replaced with rolls and the total. Formatted so that the total
appears larger than the rest of the text to make it easy to spot.

Formulas can contain any number of dice specifications and modifiers. All dice specifications are replaced
with thier totals first and then remaining math is evaluated.

Note: When rolling a single die the dice number is still required (e.g. 1D10 instead of D10). 

Example:

```/r 3D6+5```

### Roll with Name: /rn name formula

This acts the same as /r but allows a single word name to be associated with the roll. The conditions for
the formula and the output is the same except that the output is preceeded by the name. Typically the name
is used to indicate what the roll is for.

Note: name is a single word entry. If you need multiple works, use characters like - or _ to separate words.

Example:

```/rn Attack 1D20+4```

### GM Roll: /gr formula

This acts the same as /r but sends the output only to players in the GM role. Typically used to make secret
rolls for the GM without other players seeing the result. Please note that if the roll is made from a creature
then the creature will speak the roll formula (visiable to all) but not show the result.

This functionality is only available if a /w Chat Service handler is available, like the Chat Whisper Plugin.

Example:

```/gr 3D8+2```

### GM Roll with Name: /grn formula

This acts the same as /rn but sends the output only to players in the GM role. Typically used to make secret
rolls for the GM without other players seeing the result. Please note that if the roll is made from a creature
then the creature will speak the roll formula (visiable to all) but not show the result.

This functionality is only available if a /w Chat Service handler is available, like the Chat Whisper Plugin.

Example:

```/grn Damage 2D6+2```
