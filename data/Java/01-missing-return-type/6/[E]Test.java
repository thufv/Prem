public class Test {
    public void actionPerformed(ActionEvent klikk)
    {
        if (klikk.getSource() == btnMaks)
        {
            String f = txtFilnavn.getText();
            finnMaks(f);
        }
    }
     
    public static void main(String arg[])
    {
        GUI app = new GUI();
    }
     
    public finnMaks(String filnavn)  throws Exception 
    {
       // String filnavn = "C:\\Documents and Settings\\wah\\Desktop\\temperatur.txt";
         
        try
        {
            
            FileReader leseforbindelse = new FileReader(filnavn); // forbindelse
            BufferedReader fil = new BufferedReader(leseforbindelse); // format
            var maks = 0.0;
            
            // read a line
            String enLinje = fil.readLine(); 
            
            while (enLinje != null)
            {
            // 1st is a year
            String year = enLinje; 
                
            System.out.print("year: " + year + " ");
                
            // read a line
            enLinje = fil.readLine(); 
                
            // 2nd is a temperature
            String temp = enLinje; 
            System.out.println("temperature: " + temp);
                
            // "doubletemp" is "temp" in "double" format // converted
            double doubletemp = Double.parseDouble(temp);
                
            // find out the max temperature
            if (doubletemp > maks)
            {
                maks = doubletemp;
            }
                
            // read a line
            enLinje = fil.readLine(); 
            }
            fil.close();
            return maks;
        } // end try
        
        catch (FileNotFoundException e)
        {
            System.out.println(filnavn + " ikke funnet");
            return maks;
        }
            
        catch (IOException e)
        {
            System.out.println("IO-feil oppst�tt ved oppkobling av forbindelse til " + filnavn);
            return maks;
        }
         
    } // end finnMak
}