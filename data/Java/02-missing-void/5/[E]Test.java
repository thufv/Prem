import java.util.Scanner;
public class Period
{
  private static String phrase;
  public static void main(String [] args)
  {
    Scanner keyboard = new Scanner(System.in);
    String userInput;
    int[] letter = new int [27];
    int number = keyboard.nextInt();
    System.out.println("Enter a sentence with a period at the end.");
    userInput = keyboard.nextLine();
    userInput.toLowerCase();
  }
  // this is where the error is occuring at.
  public Sorter(String newPhrase)
  {
    phrase=newPhrase.substring(0,newPhrase.indexOf("."));
  }

  private int charToInt(char currentLetter)
  {
    int converted=(int)currentLetter-(int)'a';
    return converted;
  }

  private void writeToArray()
  {
    char next;
    for (int i=0;i<phrase.length();i++)
    {
      next=(char)phrase.charAt(i);
      sort(next);
    }
  }

  private String cutPhrase()
  {
    phrase=phrase.substring(0,phrase.indexOf("."));
    return phrase;
  }

  private void sort(char toArray)
  {
    int placement=charToInt(toArray);
    if (placement<0)
    {
      alphabet[26]=1;
    }
    else
    {
      alphabet[placement]=alphabet[placement]+1;
    }
  }

  public void entryPoint()
  {
    writeToArray();
    displaySorted();
  }

  private void displaySorted()
  {
    for (int q=0; q<26;q++)
    {
      System.out.println("Number of " + (char)('a'+q) +"'s: "+alphabet[q]);
    }
  }
}