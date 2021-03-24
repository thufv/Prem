# run this in `src/Prem`

dotnet run -l c# --learn ../../data/CSharp --learn-with 2-6 --bench ../../data/CSharp --bench-with 1 -o ../../logs/c#-test-1
dotnet run -l c# --learn ../../data/CSharp --learn-with 1,3-6 --bench ../../data/CSharp --bench-with 2 -o ../../logs/c#-test-2
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-2,4-6 --bench ../../data/CSharp --bench-with 3 -o ../../logs/c#-test-3
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-3,5-6 --bench ../../data/CSharp --bench-with 4 -o ../../logs/c#-test-4
dotnet run -l c# --learn ../../data/CSharp --learn-with 1-4,6 --bench ../../data/CSharp --bench-with 5 -o ../../logs/c#-test-5
dotnet run -l java --learn ../../data/Java --learn-with 2-6 --bench ../../data/Java --bench-with 1 -o ../../logs/java-test-1
dotnet run -l java --learn ../../data/Java --learn-with 1,3-6 --bench ../../data/Java --bench-with 2 -o ../../logs/java-test-2
dotnet run -l java --learn ../../data/Java --learn-with 1-2,4-6 --bench ../../data/Java --bench-with 3 -o ../../logs/java-test-3
dotnet run -l java --learn ../../data/Java --learn-with 1-3,5-6 --bench ../../data/Java --bench-with 4 -o ../../logs/java-test-4
dotnet run -l java --learn ../../data/Java --learn-with 1-4,6 --bench ../../data/Java --bench-with 5 -o ../../logs/java-test-5
