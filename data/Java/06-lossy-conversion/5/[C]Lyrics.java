import static java.lang.Math.pow;
import java.util.Scanner;

public class Lyrics
{
     public static void main(String []args)
     {
          int b;
          Scanner scan = new Scanner(System.in);
          System.out.println ("Enter a number: ");
          b = scan.nextInt();
          Cube(b);
     }
     public static int cube (int b)
     {
          int ret = (int) Math.pow (b, 3);
          return ret;
     }
}