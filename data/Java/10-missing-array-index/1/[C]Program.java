public class Program {

    public int[] arrayAdd(int[] R1, int[] R2) {
        int[] sumArray = new int[R1.length];

        if (R1.length != R2.length) {
            System.out.println("The arrays must be same length");
        } else {
            for (int i = 0; i < R1.length; i++)
            {

                sumArray[i] = R1[i]; // Error
            }
        }
        return sumArray;
    }
}