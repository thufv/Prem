import java.util.Scanner;

public class gyak {
    public static void main(String[] args){
            Scanner scanner = new Scanner(System.in);
            int a = scanner.nextInt();
            int result=0;
            for (int i=a-1;i>=1;i--){
                 result= a*i;

            }
            System.out.println(result);

        }
    }