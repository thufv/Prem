class MyMath {
    public double hero(double n1, double n2, double n3)
    {
        double n4;
        double n5;
        n4 = (n1 + n2 + n3) / 2;
        n5 = Math.sqrt((n4 * (n4 - n1) * (n4 - n2) * (n4 - n3)));
        return n5;
    }
      

    public static void main(String[] args) {
        double length_of_a;
        double length_of_b;
        double length_of_c;
        double area_of_triangle;
        
        area_of_triangle = (double) MyMath.hero(length_of_a,length_of_b,length_of_c);
    }
}
