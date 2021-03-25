import java.util.*;
public class project5
{
   public static void main (String args[])
   {
      int count = 1;
      while (count < 11) {
          Random r = new Random();
          int Low = Math.sqrt(count);
          int High = count;
          int Result = r.nextInt(High-Low) + Low;
          System.out.println( count + "\t" + Math.sqrt(count));
          count++;
       }
   }
}