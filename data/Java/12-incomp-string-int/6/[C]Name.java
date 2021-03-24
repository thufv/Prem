class fibo {

    static void Fibonacci(int Total,int n1,int n2){
    
        if(Total > 1){      
            int sum = n1 + n2;
            System.out.print(sum + " ");
            n1 = n2;
            n2 = sum;
            --Total;
            Fibonacci(Total,n1,n2);
        }
    }
    
}

class main{

    public static void main(String args[]){

    fibo f = new fibo();    
    System.out.print(0 + " ");
    System.out.print(1 + " ");
    int Total = Integer.parseInt(args[0]);
    int n1 = Integer.parseInt(args[1]);
    int n2 = Integer.parseInt(args[2]);    
    f.Fibonacci(Total,n1,n2);


    }
}