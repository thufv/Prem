import java.util.*;  

public class a 
{  

    public static void main(String[ ] args)  
    {  

        Scanner in = new Scanner(System.in);  
        System.out.println("Enter your name: ");  
        List<String> list = new ArrayList<String>( );  
        boolean loop = true;  
        while(loop)  
        {  

            String s = in.nextLine( );  

            System.out.println(s.charAt(0));




            if(s.equals("")|s.equals("pl exit"))  
            {  
                break;      
            }  
            else  
            {  
                list.add(s);  
            }  
        }  

    }//main ends  

}