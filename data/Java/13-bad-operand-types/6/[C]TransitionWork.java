package transition.work;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;

/**
 *
 * @author New
 */
public class TransitionWork {

    /**
     * @param args the command line arguments
     * @throws java.io.IOException
     */
    public static void main(String[] args) throws IOException {
        System.out.println("Hello, what is your name?");

        InputStreamReader inputStreamReader = new InputStreamReader(System.in);
    BufferedReader reader = new BufferedReader(inputStreamReader);
    System.out.println("Type name:");
    String name = reader.readLine();
    System.out.println("Hello "+name+", How old are you?");
    String age;
        age = reader.readLine();

    if (Integer.parseInt(age) < 17) {
        System.out.println("You are a adult");
        }  

    }
}