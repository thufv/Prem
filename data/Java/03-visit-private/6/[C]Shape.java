public abstract class Shape{
    public int x, y, width, height;
    public Color color;
    public Shape(int x, int y, int width, int height, Color color){
        setXY(x, y);
        setSize(width, height);
        setColor(color);
    }

    public boolean setXY(int x, int y){
        this.x=x;
        this.y=y;
        return true;
    }

    public boolean setSize(int width, int height){
        this.width=width;
        this.height=height;
        return true;
    }

    public boolean setColor(Color color){
        if(color==null)
            return false;
        this.color=color;
        return true;
    }

    public abstract void draw(Graphics g);
}

class Rectangle extends Shape{
    public Rectangle(int x, int y, int width, int height, Color color){
        super(x, y, width, height, color);
    }

    public void draw(Graphics g){
        setColor(color);
        fillRect(x, y, width, height);
        setColor(Color.BLACK);
        drawRect(x, y, width, height);
    }
}

class Ellipse extends Shape{
    public Ellipse(int x, int y, int width, int height, Color color){
        super(x, y, width, height, color);
    }

    public void draw(Graphics g){
        g.setColor(color);
        g.fillOval(x, y, width, height);
        g.setColor(Color.BLACK);
        g.drawOval(x, y, width, height);
    }
}
class Color{ 
    static Color BLACK;
}