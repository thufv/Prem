using System;
public class UserMainCode
{
    //Assume following return types while writing the code for this question. 
    public static int[,] output1 = new int[10,10];
    public static void soduko()
    {
    }
    public static void SolveSudoku(int[,] input1)
    {
        input1 = new int[9, 9];
        int i, j;
        for (i = 0; i < 9; i++)
            output1[i, 0] = input1[i, 0];
        soduko();


        //Write code here 
    }
}