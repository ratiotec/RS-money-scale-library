# RS money scale library
### About
A communication library for RS money scale devices written in C#. It supports RS 1000, 1200 and 2000 devices. The library supports task-based asynchronous and classic synchronous operations to receive accounting informations.
## Version
1.2.0
## Usage
The library consists of two files:
  - rt.Devices.RsScale.dll
  - rt.Hid.v2.dll (dependency)

You have to reference the rt.Devices.RsScale.dll assembly. Additionally you need the rt.Hid.v2.dll assembly in your output directory. You can find sourcecode samples in the included wiki pages.
### .NET applications
You can get the library by installing its [NuGet] package.
```sh
PM> Install-Package RS-money-scale-library
```
Otherwise feel free to clone and add this project to your solution.
### non .NET applications
You can clone this repository and compile it with VS 2015. Further you find the release build inside the [Build-output] folder. Use wrappers to bridge the library to other languages as .NET based languages. This makes you able to import operations in native C++ or Java applications as well.
## Requirements
You need a installed framework version 4.5 to run the library properly. At the moment we do not support .NET 4.0 (or lower) any longer. If you have the requirement to run .NET 4.0 only you should be able to use Microsoft's upgrade package [Microsoft.Bcl.Async] to support task operations.
## Technical support
Feel free to create new issues as feature request or bug reports in this repository.
There are also samples in the included [wiki pages].
## License
[MIT]

[NuGet]: <https://www.nuget.org/packages/RS-money-scale-library>
[Microsoft.Bcl.Async]: <https://www.nuget.org/packages/Microsoft.Bcl.Async>
[MIT]: <https://opensource.org/licenses/MIT>
[Build-output]: <https://github.com/ratiotec/RS-money-scale-library/tree/master/Build-output>
[wiki pages]: <https://github.com/ratiotec/RS-money-scale-library/wiki>
