class Slide {
    public static void move(int row, int col) 
    {
        char [][] temp= new char [cells.length][]; 
        for (int i= 0; i< cells.length; i++) {
            int destRow = (i+row)%cells.length;
            temp[destRow] = new char [cells[i].length];
            for (int j= 0; j < cells[i].length; j++)
                temp[destRow][(j+col)%cells[i].length] = cells[i][j];
            }
              cells= temp;
    }

    static void method () {
        move(0,0);
    }
}
