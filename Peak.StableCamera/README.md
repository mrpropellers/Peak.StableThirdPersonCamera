# Stable Camera
Stabilizes the camera and uses contextual third-person views to prevent motion sickness.
Press *N* (configurable) to toggle the stabilization on or off in-game. Stabilization is on by default.

# Features and Configuration
The game needs to be launched with the mod at least once before the configuration file will appear
in the BepInEx folder or in the mod manager.

* **Enabled**
	- Enables the mod. Setting this to false effectively disables the entire mod.
	- The toggle key can be used to change this option in-game.
	- Default: True
* **Toggle Key**
	- The shortcut key which toggles the mod on or off in-game.
	- Default: N
* **Stabilize Tracking**
	- Reduce camera wobble using stabilized tracking. Disable this if head clipping is more annoying than wobble.
	- Default: True
* **Tracking Power**
	- Option to control how aggressively the camera will follow the character. Higher values lead to more wobble, but less clipping.
	- Default: 2.0
* **Third-Person Ragdoll**
	- Switch to a third-person perspective whenever the character ragdolls.
	- Default: True
* **Extra Climbing FOV**
	- Controls how much the camera's field of view expands while climbing.
	- A value of 0 prevents the FOV from changing; 40 is the game's original value.
	- **NOTE:** *The PEAK devs have since added an identical option for this in the base game settings,
	  but this option remains in the mod to preserve the out-of-the-box experience. It overrides the base game's new setting.*
	- Default: 0.0
* **Dizzy Effect Strength**
	- Option to adjust the strength factor of the dizzy camera effect, e.g. when recovering from passing out.
	- A value of 0.0 disables the dizzy camera effect; 1.0 leaves the effect unchanged.
	- The dizzy effect is always disabled if Third-Person Ragdoll is enabled.
	- Default: 0.0
* **Shake Effect Strength**
	- Option to adjust the strength factor of the camera shake effect, e.g. when stamina is exhausted while climbing.
	- A value of 0.0 disables the camera shake effect; 1.0 leaves the effect unchanged.
	- Default: 0.0

## Bugs / Contact
The fastest way to contact me is on Discord.
You can find me in the Lethal Company Modding or R.E.P.O. Modding Discord servers,
or in my [Deja Drift Discord Server](https://discord.gg/yKwt2AWcGF)