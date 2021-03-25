import java.util.Scanner;

class Cat1
{
	public String name;

	//***********************************************************

	public void setName(String name)
	{
		this.name = name;
	}

	//***********************************************************

	public String getName()
	{
		return this.name;
	}
} // end class Cat1

public class Cat1Driver
{
	public static void main(String[] args)
	{

		Scanner stdIn = new Scanner(System.in);
		Cat1 myCat1 = new Cat1();
		String name;


		System.out.println("Enter the name of Cat1:");
		myCat1.name = stdIn.next();

	} // end main
} // end class Cat1Driver