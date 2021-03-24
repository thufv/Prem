class MyClass {
    char[][] lab = new char[100][100];
    public void func(){
        try {
            FileReader fr = new FileReader ("Laberinto.txt");
            BufferedReader br = new BufferedReader(fr);

            String s,str;
            String[] buffer;

            int y=0;

            while ((s=br.readLine())!= null){
                StringBuilder builder = new StringBuilder();
                str=builder.append(s).toString();
                buffer=str.split("\t");

                for (int x=0;x<str.length();x++){
                    this.lab[x][y]=Integer.parseInt(str[x]);
                }

                y++;
            }
        }
        catch(Exception e){

        }
    }

}