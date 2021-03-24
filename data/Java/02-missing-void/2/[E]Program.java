class Test {
    int x, y;

    calc(int a) {
        x = a;
        System.out.println("Square is " + (x * x));
    }

    calc(int a, int b) {
        x = a;
        y = b;
        System.out.println("Addition is " + (x + y));
    }
}

class Main {
    public static void main(String[] args) {
        Test obj = new Test();
        obj.calc(10, 20);
        obj.calc(10);
    }
}