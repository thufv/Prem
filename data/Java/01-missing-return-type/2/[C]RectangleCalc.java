public class RectangleCalc {
    private double length;
    private double width;


    public RectangleCalc(double length, double width){
        this.length = length;
        this.width = width;
    }

    public double getArea() {
        double area = length * width;
        return area;
    }
}