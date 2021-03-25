public class FileReaderProgram {
    static String stringComponent;
    int integerComponent;

    public static void main(String[] args) {
        Scanner in = new Scanner(System.in);
        System.out.println("Enter the absolute path of the file");
        String fileName = in.next(); // Gets the file from the user
        File inFile = new File(fileName);

        Scanner fileReader = new Scanner(inFile); // Constructs Scanner for reading the file

        while (fileReader.hasNextLine()) {
            String line = fileReader.nextLine(); // Gets line from the file
            Scanner lineScanner = new Scanner(line); // Constructs new scanner to analize the line
            String stringComponent = lineScanner.next(); // Obtains the first word of the data line
            while (!lineScanner.hasNextInt()) // Checks if there is another word in the string portion of the line
            {
                stringComponent = stringComponent + " " + lineScanner.next();
            }
            int integerComponent = lineScanner.nextInt(); // Obtains the integer part of the data line
            lineScanner.nextLine(); // Consume the newline
        }

        System.out.println(stringComponent);
        System.out.println("integerComponent");

    }

}