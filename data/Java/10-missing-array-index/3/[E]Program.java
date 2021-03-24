public class Program {
    public Program(int[] g, String[] n, int num)
    {
        // **instantiate both arrays with 'num' objects
                String []names=new String[num];
                int[] grades=new int[num];
        //**use a for-loop to assign each name and grade with the incoming values
                for(int i=0;i<num;i++)
                {
                    names[i]=n[i];
                    grades=g[i];
                }
    }
}