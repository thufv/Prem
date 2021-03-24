import java.io.Console;
class Game {
    public static final int MAX_MISSES = 7;
    private String mAnswer;
    private String mHits;
    private String mMisses;
    
    public Game(String answer) {
      mAnswer = answer;
      mHits = "";
      mMisses = "";
     
    }
    
    public boolean applyGuess(char letter) {
      boolean isHit = mAnswer.indexOf(letter) >= 0;
      if (isHit) {
        mHits += letter;
      } else {
        mMisses += letter;
      }
      return isHit;  
    }
    
    public String getCurrentProgress() {
      String progress = "";
      for (char letter: mAnswer.toCharArray()){
        char display = '-';
        if (mHits.indexOf(letter) >= 0) {
          display = letter;
        }
        progress +=display;
      }
    return progress;
    }
    
    public int getRemainingTries(){
      return MAX_MISSES - mMisses.length();
    }
  }

public class Prompter {
  private Game mGame;
  
  
  public Prompter(Game game) {
    mGame = game;
  }
  
  public void play() {
    while (mGame.getRemainingTries>0) {
      displayProgress();
      promptForGuess();
    }
  }

  
  public boolean promptForGuess() {
    Console console = System.console();
    String guessAsString = console.readLine("Enter a letter: ");
    char guess = guessAsString.charAt(0);
    return mGame.applyGuess(guess);
  }
  
  public void displayProgess() {
    System.out.printf("You have %d tries left to solve:  %s\n",
                      mGame.getRemainingTries(),
                      mGame.getCurrentProgress());
  }
  
}