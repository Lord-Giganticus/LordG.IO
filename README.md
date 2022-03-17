# LordG.IO
A Library for various formats from the Wii, Wii U, and Switch Era

This Library takes code from other C# Repos in order to provide useable code.

## Other Repos used
* [Switch-Toolbox](https://github.com/KillzXGaming/Switch-Toolbox)


# Version History
## 1.0.0
* Inital Publish to nuget.pkg.github.com
## 1.0.1
* `EndianReader` and `EndianWriter` no longer have unsafe methods internally.
* `EndianReader` and `EndianWriter` now override most methods from `BinaryReader` and `BinaryWriter`.
* `EndianStream` reworked to promote using `EndianReader` and `EndianWriter` more.

# Downloading
You can always find the latest nuget package on the [pakages](https://github.com/Lord-Giganticus/LordG.IO/packages/1233326) page.<br>
It's more recommended to just download the package and add it as a nuger source via file path than try to add the web link