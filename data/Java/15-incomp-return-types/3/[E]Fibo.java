class Fibo{

    public static void main(String[] args){
        int a=0 ,b=1,c=1 ;
        for(int i=0; i<=4; i++){
    
            c=a+b ;
            c=a ;
            a=b;
        }
        return c ;  
    
    }
}