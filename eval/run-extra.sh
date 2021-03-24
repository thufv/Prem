# run this in `src/Prem`
dotnet run -l c# --load ../../eval/results/c#-train-5/RuleLib.xml --predict ../../data/Extra ../../data/Mutation -o ../../logs/c#-extra --int --int-in ../../eval/interactive-commands.txt

# do the following if you want to run extra testcases (105) only:
# dotnet run -l c# --load ../../eval/results/c#-train-5/RuleLib.xml --predict ../../data/Extra -o ../../logs/c#-extra-only --int --int-in ../../eval/interactive-commands.txt
