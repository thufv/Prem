public class ScrabblePlayer {
    private String mHand;

    public ScrabblePlayer() {
        mHand = "";
    }

    public String getHand() {
        return mHand;
    }

    public void addTile(char tile) {
    // Adds the tile to the hand of the player
        mHand += tile;
    }

    public boolean hasTile(char tile) {
        return mHand.indexOf(tile) > -1;
    }
    public void getTileCount(char tile){
    int counter = 0;
    for (char t: mHand.toCharArray()){
        if( tile == t ){
            counter ++;
        }
    }  
    return counter;

    }
}