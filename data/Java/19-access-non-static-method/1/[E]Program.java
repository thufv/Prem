class Program {
    private java.util.List<String> someMethod(){
        /* Some Code */
        return someList;            
    }
    
    public static void main(String[] strArgs){          
         // The following statement causes the error. You know why..
        java.util.List<String> someList = someMethod();         
    }
}