public class Student {

    private String firstName;
    private String lastName;
    private int score1, score2, score3;
    private int average;



      public void setFirstName(String name){
      firstName = name;
    }
      public String getFirstName(){
      return firstName;
    }
      public void setLastName(String name){
      lastName = name;
    }
      public String getLastName(){
      return lastName;
    }
      public void setScore1(String newvalue){
      score1 = Integer.parseInt(newvalue);
    }
      public int getScore1(int newvalue){
      return score1;
    }
      public void setScore2(String newvalue){
      score2 = Integer.parseInt(newvalue);
    }
      public int getScore2(int newvalue){
      return score2;
    }
      public void setScore3(String newvalue){
      score3 = Integer.parseInt(newvalue);
    }
      public int getScore3(int newvalue){
      return score3;
    }
      public int setAverage(int newvalue){
      average = (score1 + score2 + score3)/3;
    }
      public int getAverage(String newvalue){
      return average;
    } 
}