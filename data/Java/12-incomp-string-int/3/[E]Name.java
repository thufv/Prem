import java.util.Scanner;

public class Name {

public static void main(String args[]) {

    Scanner in = new Scanner(System.in);
    String userName;
    int userAge;

    System.out.println("What is your full name?");
    userName = in.nextLine();

    System.out.println("What is your age?");
    userAge = in.nextLine();

    System.out.println("You're " + userName + " and you are " + userAge + "   years old");
   }
}