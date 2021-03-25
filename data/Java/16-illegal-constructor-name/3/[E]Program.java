class Bicycle
{
    // class-level variables
    private int gear;
    private int cadence;
    private int speed;

    // constructor
    public Bike(int startCadence, int startSpeed, int startGear) {
        gear = startGear;
        cadence = startCadence;
        speed = startSpeed;
    }

    // another method (not a constructor)
    public void ShiftUp() {
        gear = gear + 1; // notice the 'gear' variable is available here too.
    }
}