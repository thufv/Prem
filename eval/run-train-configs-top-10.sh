# run this in `src/Prem`

dotnet run -l c# --learn ../../data/CSharp --learn-with 1-2 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-2
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-3 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-3
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-4 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-4
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-5 --bench ../../data/CSharp --bench-with 6 -o ../../logs/c#-train-5
dotnet run -l java --learn ../../data/Java --learn-with 1-2 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-2
dotnet run -l java --learn ../../data/Java --learn-with 1-3 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-3
dotnet run -l java --learn ../../data/Java --learn-with 1-4 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-4
dotnet run -l java --learn ../../data/Java --learn-with 1-5 --bench ../../data/Java --bench-with 6 -o ../../logs/java-train-5
