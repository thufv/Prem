import java.util.Scanner;

public class TrainTicket
{
    public static void main (String args[])
    {

        Scanner money = new Scanner(System.in);
        System.out.print("Please type in the type of ticket you would like to buy.\nA. Child B. Adult C. Elder.");
        String type = money.next();
        System.out.print("Now please type in the amount of tickets you would like to buy.");
        int much = money.nextInt();
        int price = 0;
        switch (type)
        {
            case "A":
            price = 10;
            break;
            case "B":
            price = 60;
            break;
            case "C":
            price = 35;
            break;
            default:
            price = 0;
            System.out.print("Not a option ;-;");
        }
        if (price!=0)
        {
            int total2 = (int) price* much* 0.7;
            System.out.print("Do you have a coupon code? Enter Y or N");
            String YN = money.next();
            if (YN.equals("Y"))
            {
                System.out.print("Please enter your coupon code.");
                int coupon = money.nextInt();
                if(coupon==21)
                {
                    System.out.println("Your total price is " + "$" + total2 + ".");
                }
                else
                {
                    System.out.println("Invalid coupon code, your total price is " + "$" + price* much + ".");
                }
                }
                else
                {
                    System.out.println("Your total price is " + "$" + price* much + "." ); 
            }
        }

        money.close();
    }
}