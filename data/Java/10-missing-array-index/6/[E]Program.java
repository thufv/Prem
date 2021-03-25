public class Program {
    public static void main(String[] args) {
        double[] myList = { 1.9, 2.9, 3.4, 3.5 };
        double out = myList[1];
        double add = myList[2];

        for (int i = 0; i < myList.length; i++) {
            myList[i] = out;
            System.out.println(myList[i] + " ");
        }

        double total = 0;
        for (int j = 0; j < myList.length; j++) {
            myList = add;
            total += myList[j];
        }
        System.out.println("Total is " + total);

        double max = myList[0];
        for (int k = 1; k < myList.length; k++) {
            myList[k] = maxi;
        }
        System.out.println("Max is " + max);
    }
}