# DocsPortingTool

This repo contains tools that allow porting documentation in two directions:

- IntelliSense xml files to MS Docs xml files. [Instructions](docs/PortToDocs.md).

- MS Docs xml files to triple slash comments in source code. [Instructions](docs/PortToTripleSlash.md).

## Requirements

- [.NET 6.0 SDK](https://github.com/dotnet/installer#installers-and-binaries)
- A local git clone of a dotnet repo with source code whose APIs live in an API docs repo. Examples:
  - [dotnet/runtime](https://github.com/dotnet/runtime)
  - [dotnet/winforms](https://github.com/dotnet/winforms)
  - [dotnet/wpf](https://github.com/dotnet/wpf)
  - [dotnet/wcf](https://github.com/dotnet/wcf)
- A local git clone of the API docs repo where the above project hosts its documentation. For example, all the repos listed above host their documentation in the [dotnet-api-docs repo](https://github.com/dotnet/dotnet-api-docs).

## Install as dotnet tools

To install the two tools as global dotnet tools in your `$PATH`, run the `install-as-tool.ps1` script.

Documentation for global dotnet tools: https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install

Remember to update the tool periodically to collect the latest changes. Updating instructions: https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-update
