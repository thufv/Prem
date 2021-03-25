import javabook.*;              //Same result Scanner or javabook. Tried both and it worked.
import java.util.Scanner;       //this is required
class App
{
    public static void main(String args[])
    {


        //declare variable
        String theNumber;

        //declare object
        Scanner someInput;

        //input
        System.out.println("Please enter area size : ");
        someInput = new Scanner(System.in);
        theNumber = someInput.nextLine();

        //processing

        if ( theNumber < 20 )
        {
            System.out.println( "It is too small." ) ;
        }
        // Other Codes ...

        //close the program without error
        System.exit(0);
    }
}