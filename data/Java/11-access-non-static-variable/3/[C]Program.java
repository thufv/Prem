class FirstClass {
    static int idNo = 25;

    public static void print() {
        System.out.println("firstclass citizen " + idNo);
    }
}

class SecondClass {
    int idNo = 24;

    public static void print() {
        System.out.println("secondclass citizen" + idNo);
    }
}

class People {
    // FirstClass female;
    // SecondClass male;
    public static void main(String[] args) {
        System.out.println("people from java world");
        FirstClass.print();
        SecondClass.print();
    }
}