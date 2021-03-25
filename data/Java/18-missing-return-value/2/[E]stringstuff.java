public class stringstuff{

    //using charAt    
    public static String ReverseF(String n){
        String ret = "";
        int len = n.length();
        for (int i = 0; (i < n.length()); i++){
            ret += (n.charAt(len - i - 1));
        }
        System.out.println(ret);
        return;
    }

    public static void main(String[]args){
        ReverseF("Hello");
    }
}