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
## 1.0.2
* Removed useless unsafe from internal `EndianWriter` method and 2 methods to `Util`
## 1.1.0
* Updated to `netstandard2.1`.
* `Yaz0` now has a decompress method for `ref Span<byte>`.
* Small optimizeations across multiple files.

# Downloading
You can always find the latest nuget package on the [packages](https://github.com/Lord-Giganticus/LordG.IO/packages/1233326) page.<br>
It's more recommended to just download the package and add it as a nuget source via file path than try to add the web link
