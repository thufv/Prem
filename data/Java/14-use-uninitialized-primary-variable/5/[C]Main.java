class Main
{
    public boolean moveRandom(char ch)
    {
        boolean check = false;
        int row=0, col;
        while (check)
        {
            row = getRandPos.nextInt(dimension);
            col = getRandPos.nextInt(dimension);
            if (board[row][col] != 'X' && board[row][col] != 'O')
                check = true;
        }
        board[row][col] = ch;
        return true;
    }

}
