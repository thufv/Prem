import java.io.File;
import java.io.FileNotFoundException;
import java.util.ArrayList;
import java.util.Scanner;
import java.io.PrintWriter;

public class Main
{
    public static void main (String[] args) throws FileNotFoundException
    {
        ArrayList<Double> RPM = new ArrayList<Double>(), bCoeffs, filteredRPM = new ArrayList<Double>();
        Scanner RPMFile = new Scanner(new File("RotationSpeed.txt"));

        while(RPMFile.hasNextLine()){
            String line = RPMFile.nextLine();

            Scanner scanner = new Scanner(line);
            scanner.useDelimiter(",");
            while(scanner.hasNextDouble()){
                RPM.add(scanner.nextDouble());
            }
            scanner.close();
        }
        RPMFile.close();

        int windowSize = 10;
        int filterItterations = 1;

        for (int i = 0; i < windowSize; i++){
                double temp = 1/windowSize;
                bCoeffs.add(temp);
        }

        for (int k = 1; k <= filterItterations; k++){
            if (k == 1){
                for (int n = windowSize; n < RPM.size(); n++){
                    int m = 0;
                    double tempYSum = 0;
                    for (int j = 0; j < windowSize; j++){
                        double tempY = (bCoeffs.get(j))*(RPM.get(n-m));
                        tempYSum += tempY;
                        m++;
                    }
                    filteredRPM.add(tempYSum);
                }
            }else{
                int i = 1;
                for (int n = windowSize; n < filteredRPM.size(); n++){
                    int m = 0;
                    double tempYSum = 0;
                    for (int j = 0; j < windowSize; j++){
                        double tempY = (bCoeffs.get(j))*(filteredRPM.get(n-m));
                        tempYSum += tempY;
                        m++;
                    }
                    filteredRPM.set(i, tempYSum);
                    i++;
                }
            }
        }
    }
}