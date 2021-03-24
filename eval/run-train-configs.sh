# run this in `src/Prem`

## top-1
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-2 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-2-top-1 -k 1
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-3 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-3-top-1 -k 1
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-4 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-4-top-1 -k 1
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-5 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-5-top-1 -k 1
dotnet run -l java --learn ../../data/Java --learn-with 1-2 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-2-top-1 -k 1
dotnet run -l java --learn ../../data/Java --learn-with 1-3 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-3-top-1 -k 1
dotnet run -l java --learn ../../data/Java --learn-with 1-4 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-4-top-1 -k 1
dotnet run -l java --learn ../../data/Java --learn-with 1-5 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-5-top-1 -k 1

## top-3
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-2 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-2-top-3 -k 3
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-3 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-3-top-3 -k 3
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-4 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-4-top-3 -k 3
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-5 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-5-top-3 -k 3
dotnet run -l java --learn ../../data/Java --learn-with 1-2 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-2-top-3 -k 3
dotnet run -l java --learn ../../data/Java --learn-with 1-3 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-3-top-3 -k 3
dotnet run -l java --learn ../../data/Java --learn-with 1-4 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-4-top-3 -k 3
dotnet run -l java --learn ../../data/Java --learn-with 1-5 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-5-top-3 -k 3

## top-5
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-2 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-2-top-5 -k 5
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-3 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-3-top-5 -k 5
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-4 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-4-top-5 -k 5
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-5 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-5-top-5 -k 5
dotnet run -l java --learn ../../data/Java --learn-with 1-2 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-2-top-5 -k 5
dotnet run -l java --learn ../../data/Java --learn-with 1-3 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-3-top-5 -k 5
dotnet run -l java --learn ../../data/Java --learn-with 1-4 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-4-top-5 -k 5
dotnet run -l java --learn ../../data/Java --learn-with 1-5 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-5-top-5 -k 5

## top-7
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-2 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-2-top-7 -k 7
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-3 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-3-top-7 -k 7
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-4 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-4-top-7 -k 7
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-5 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-5-top-7 -k 7
dotnet run -l java --learn ../../data/Java --learn-with 1-2 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-2-top-7 -k 7
dotnet run -l java --learn ../../data/Java --learn-with 1-3 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-3-top-7 -k 7
dotnet run -l java --learn ../../data/Java --learn-with 1-4 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-4-top-7 -k 7
dotnet run -l java --learn ../../data/Java --learn-with 1-5 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-5-top-7 -k 7

## top-10 (default)
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-2 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-2
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-3 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-3
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-4 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-4
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-5 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-5
dotnet run -l java --learn ../../data/Java --learn-with 1-2 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-2
dotnet run -l java --learn ../../data/Java --learn-with 1-3 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-3
dotnet run -l java --learn ../../data/Java --learn-with 1-4 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-4
dotnet run -l java --learn ../../data/Java --learn-with 1-5 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-5
