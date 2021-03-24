# PREM: Program Repair using Error Messages

## System Requirements

Before building PREM, the following platforms/tools are required as external dependencies. All libraries that PREM depends on are specified in the project files and will be proceed automatically by `dotnet`.

### .NET

.NET is a cross-platform development environment. PREM is built as a `netcoreapp2.0`. To build and run PREM, you need to install a .NET Core SDK with version 2.x. The recommended version is [v2.1.26](https://dotnet.microsoft.com/download/dotnet/2.1). After installation, use `dotnet --version` to check the version number.

### UTrans Parser

PREM relies on UTrans, an external parser (for C# and Java) developed with [ANTLR](https://www.antlr.org) in Java. The standalone `jar` file is provided in the `lib/` folder. Please make sure that you put this jar somewhere on your file system, and let the environment variable `UTRANS_JAR` point to the full path of this jar, e.g. by adding `export UTRANS_JAR=<full path to UTrans-1.0.jar>` in your shell profile: `bashrc`, `zshrc`, or whatever.

### C# Compiler

To allow PREM invoke C# compiler from command line to check if a repaired program compiles, you should have `csc` on your system PATH. You may use the one supported by .NET (if any) or [Mono](https://www.mono-project.com) instead.

## Building & Running

In the project root directory:

```sh
cd src/Prem/
dotnet build
```

Type `dotnet run` to see command line option helps.

Please remember that every `dotnet` command must be executed under the folder `src/Prem/`, otherwise it cannot resolve the correct project file.

### Dataset Folder Structure

Datasets passed to options `--learn` and `--bench` must have the following folder structure:
```
<root>/
     example_group_1/
         1/
             [E]* erroneous version
             [C]* corrected version
             [P]* error info
         2/
             ...
     example_group_2/
         ...
     ...
```

PREM recognizes a file with prefix `[E]` as the erroneous version, `[C]` as the corrected version, and `[P]` as the error info (first line error position, second line error message).

To make filters `--learn-with` and `--bench-with` work normally, you must have your example folder (itself is a subfolder of some example group folder) named as a digit, e.g. `2` rather than `example-2`.

Datasets passed to option `--predict` must have the following folder structure:
```
<root>/
    case_1/
        [E]* testcase
        [P]* error info
    case_2/
    ...
```
