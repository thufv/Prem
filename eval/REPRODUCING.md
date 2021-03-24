# Reproducing Guide

This document introduces how to reproduce the experimental evaluations mentioned in the paper.

## Datasets

The `data/` folder contains all necessary examples for reproducing the experimental evaluations:
- `CSharp/` contains the initial 20 example groups in C#, for RQ1.
- `Java/` contains the initial 20 example groups in Java, for RQ1.
- `Mutation/` contains the 2000 testcases generated via mutation, for RQ2.
- `Extra/` contains the additional 105 testcases used in incremental learning, for RQ3.
- `Seeds/` provide all seed programs, just for reference (not used in any experiment).

For `CSharp/` and `Java/`, every example consists of four files:
- the erroneous program (filename prefix `[E]`),
- the expert-suggested correction (filename prefix `[C]`),
- the formatted error message (filename prefix `[P]`), line number (first line) followed by error message (second line), and
- the resource (i.e. the url where we collected this example) and the raw error information (filename prefix `[M]`, not recognized by PREM).

## Reproducing Steps

Following the appearance order of the research questions in the paper:

### Learning Ability (RQ1)

In `src/Prem/`, execute `sh ../../eval/run-train-configs.sh`. This script starts experiments under 20 configurations in combination of two dimensions: (1) "top-`k`" (where `k` = 1, 3, 5, 7, 10): `k` top-ranked fixing rules are saved to the rule library (if the total number of synthesized fixing rules do not exceed `k`, then save them all) for each synthesis problem; (2) "train-n" (where n = 2, 3, 4, 5): use the first `n` examples for synthesis in each example group. The learned rules are used to test against the testcases, which are the last example for each example group.

Results are output to the `logs/` folder (will be automatically created if not exist). Experiments of different configurations are saved in separated subfolders. The naming format is `<lang>-train-<n>-top-<k>`, e.g. `c#-train-5-top-7` refers to the experiment under configuration `top-7` and `train-5` for `CSharp` dataset. The suffix `top-k` for `10` is neglected because it is the default setting. In each subfolder, `learn.json` records the synthesis information, and `bench.json` records the testing results.

Since 20 configurations take a long time to finish, you may alternatively only reproduce the experiments that vary the second configuration dimension "train-n", while fixing "top-k" as 10 (the default value), using the script `../../eval/run-train-configs-top-10.sh`.

In addition to the above main experiments, we conducted a six-fold cross validation where every example (from 1 to 6) in the example group is used as the test case. In these experiments, all the other five examples are used as learning examples, and "top-k" is set to 10 (the default value). To reproduce them, execute the script `../../eval/run-six-fold.sh`. Note that the situation where the sixth (last) example is the testcase is already covered, this script conducts the others. Again, results are output to subfolders of the `logs/` folder. The naming format is `<lang>-test-<i>`, e.g. `c#-test-2` refers to the situation where the second example is used as the testcase for `CSharp` dataset.

### Generalization Ability (RQ2)

In `src/Prem/`, execute `sh ../../eval/run-mutation.sh`. This script performs the evaluation on the mutation-based dataset `Mutation/`. This evaluation uses the rules synthesized from a previous experiment `c#-train-5`. This rule library is already included in `eval/results/c#-train-5`, so that you can  reproduce this experiment independently. Testing results of all testcases will be written to `logs/c#-mutation/predict.json`.

### Extensibility via Incremental Learning (RQ3)

Strictly speaking, this evaluation needs human efforts. Even though, we hacked PREM to accept user inputs from a text file, instead of stdin. The user input commands are saved to `eval/interactive-commands.txt`. Moreover, an expert user is obligated to provide fixes for failed testcases. To simplify reproducing steps, those fixes are already included in the dataset (the fixed program is under the same folder of the testcase). Therefore, you simply first execute `sh ../../eval/run-extra.sh` in `src/Prem/`, and then check the results `logs/c#-extra/predict.json`.

If you wish to have a closer look at how user interaction is performed in PREM, first remove the option `--int-in ../../eval/interactive-commands.txt` from the script; then start this script: every time PREM asks for an user input, copy one line from `eval/interactive-commands.txt`.

### Comparison to DeepFix (RQ4)

Since the system requirements of deep learning is quite different from "normal" command line applications, the reproducing package of this evaluation can be found [here](https://github.com/thufv/DeepFix-CS).

### Our Results

All of our evaluation results are provided in subfolders of the `results/` folder. Naming rules are the same as above.
