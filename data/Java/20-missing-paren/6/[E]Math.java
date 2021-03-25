class Math 
{
    public static void main(String args[])
    {
        int sum;
        if(args[1].equalsIgnoreCase("+")
        {
        sum = Integer.parseInt(args[0]) + Integer.parseInt(args[2]);
        System.out.println("The answere is : " + sum);
        }
        else if(args[1].equalsIgnoreCase("x"))
        {
        sum = Integer.parseInt(args[0]) * Integer.parseInt(args[2]);
        System.out.println("The answere is : " + sum);
        }
        else if(args[1].equalsIgnoreCase("-"))
        {
        sum = Integer.parseInt(args[0]) - Integer.parseInt(args[2]);
        System.out.println("The answere is : " + sum);
        }
        else if(args[1].equalsIgnoreCase("/"))
        {
        sum = Integer.parseInt(args[0]) / Integer.parseInt(args[2]);
        System.out.println("The answere is : " + sum);
        }
        else
        {
        System.out.println("Something seems to be wrong, please try again.");
        }
    }
}