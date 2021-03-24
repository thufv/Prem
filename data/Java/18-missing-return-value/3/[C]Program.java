class Program{
    public static boolean compare(String h, char x){

        boolean flag = false;
        int counter = 0;
    
    
        for(int i = 0; i < h.length(); i++){
          counter++;
          if(h.charAt(i) == 'o'){
              flag = true;
          }
          else
              flag = false;
        }
         return flag;
    }
}
