class Program{
    public void fillMachine(Game game)
    {
        // Colours of balls are evenly spread between these colours,
        // in ascending order.
        Color [] colourGroupColours
            = new Color [] {Color.red, Color.orange, Color.yellow,
                        Color.green, Color.blue, Color.pink,
                        Color.magenta};
        // This happiness change will show up when the GUI is added.

        Color ballColour;
        int noOfBalls = game.getMachineSize();
        for (int count = 1; count <= noOfBalls; count++)
        {
            // The colour group is a number from 0
            // to the number of colour groups - 1.
            // For the nth ball, we take the fraction
            // (n - 1) divided by the number of balls
            // and multiply that by the number of groups.
            int colourGroup = (int) ((count - 1.0) / (double) noOfBalls
                                * (double) colourGroupColours.length);
            ballColour = colourGroupColours[colourGroup];
            game.machineAddBall(makeNewBall(count, ballColour));
        } // for

    } // fillMachine
    public static void Main() {
        Game game1 = new Game();
        fillMachine(game1);
    }
}
class Game{

}