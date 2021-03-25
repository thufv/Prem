class ProductionWorker extends Employee
{
    public int shift;
    private double rateOfPay;
    public double hoursWorked;

    ProductionWorker(String name, int id, int shift, double rateOfPay, double hoursWorked)
    {
        super(name, id);
        this.shift = shift;
        this.rateOfPay = rateOfPay;
        this.hoursWorked = hoursWorked;
    }

}

class TeamLeader extends ProductionWorker
{
    private double monthlyBonus;

    TeamLeader(String name,int id, int shift, double rateOfPay, double hoursWorked, double monthlyBonus)
    {
        super(name, id , shift, rateOfPay, hoursWorked);
        this.monthlyBonus = monthlyBonus;

    }

    public double calcPay()
    {
        double pay = 0;
        //night shift
        if (shift == 2)
        {
            pay = ((hoursWorked + hoursWorked / 2) * rateOfPay) + monthlyBonus;
        }
        else
        {
            pay = (hoursWorked * rateOfPay) + monthlyBonus;
        }
        return pay;
    }
}
class Employee{
    String name;
    int id;
    Employee(String name,int id){
        this.name = name;
        this.id = id;
    }
}