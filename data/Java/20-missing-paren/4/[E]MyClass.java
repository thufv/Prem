class MyClass {
    private static double volume(String solidom, double alturam, double areaBasem, double raiom) {
        double vol;
        
            if (solidom.equalsIgnoreCase("esfera"){
                vol=(4.0/3)*Math.pi*Math.pow(raiom,3);
            }
            else {
                if (solidom.equalsIgnoreCase("cilindro")) {
                    vol=Math.pi*Math.pow(raiom,2)*alturam;
                }
                else {
                    vol=(1.0/3)*Math.pi*Math.pow(raiom,2)*alturam;
                }
            }
            return vol;
        }
}