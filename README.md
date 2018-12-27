Prem: Program Repair using Error Messages
===

Configure UTrans
-----
Make sure that the environment variable `UTRANS_JAR` refers to the absolute path of UTrans jar.
You may add `export UTRANS_JAR=<complete path to UTrans-1.0.jar>` in your shell profile: `bashrc`, or `zshrc`, whatever.

Build
------

```
cd Prem/
dotnet build
```

Run Benchmarks
------

Usage: In directory `Prem/`, type
```
dotnet run [options] <benchmark suite>
```

Options:
`-l <language>`: REQUIRED. Specify the language of the benchmarks, such as `c#` and `java`. Same with the one specified by `UTrans`.
`-k <top-k>`: Synthesize top-k rules. DEFAULT: 1.
`-L <log level>`: Log level, FINE/DEBUG/INFO/WARNING/ERROR. DEFAULT: DEBUG.
`--learn <learning set>`: REQUIRED. Specify which examples are used as the learning set, e.g. `1-3,5`
meaning the learning set `{1,2,3,5}`.
`--test <testing set>`: Specify which examples are used as the testing set. If not specify, the testing set is empty, i.e. synthesis only without testing.
`-o <output dir>`: Specify the output directory for the experiment results (JSON files). DEFAULT: current directory.

Argument:
`<benchmark suite>`: REQUIRED. Specify the root directory of the benchmark suite, i.e. the directory which contains many benchmarks.

For example,
```
dotnet run -l c# -k 10 -L FINE -o ../logs --learn 1-3 --test 4-6 Compile.Error.Benchmarks/CSharp
```

Running one benchmark only: Specify the option `-b`, then the argument is interpreted as the root directory of the benchmark.
For example,
```
dotnet run -l c# -k 10 -L FINE -o ../logs --learn 1-3 --test 4-6 -b Compile.Error.Benchmarks/CSharp/CS0120
```