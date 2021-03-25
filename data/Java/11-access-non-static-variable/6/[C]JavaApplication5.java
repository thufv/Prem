package javaapplication5;

import java.net.*;
import java.io.*;
import java.util.Scanner;

/**
 *
 * @author preferreduser
 */
public class JavaApplication5 {
    static int fast = 0;

    public static void main(String[] args) throws IOException {
        Scanner x = new Scanner(System.in);
        System.out.print("Yun ip: ");
        String IP = x.nextLine();
        System.out.println("Loding...");
        try {
            // Create a URL for the desired page
            URL url = new URL("http://" + IP + "/arduino/digital/13/1");

            // Read all the text returned by the server
            BufferedReader in = new BufferedReader(new InputStreamReader(url.openStream()));
            in.close();
        } catch (MalformedURLException e) {
        } catch (IOException e) {
        }
        try {
            // Create a URL for the desired page
            URL url = new URL("http://" + IP + "/arduino/digital/13/0");

            // Read all the text returned by the server
            BufferedReader in = new BufferedReader(new InputStreamReader(url.openStream()));
            in.close();
        } catch (MalformedURLException e) {
        } catch (IOException e) {
        }
        System.out.println("Connected to YUN on " + IP);
        OUTER: while (true) {
            Scanner y = new Scanner(System.in);
            System.out.print("> ");
            String str = y.nextLine();
            switch (str) {
            case "on":
                try {
                    // Create a URL for the desired page
                    URL url = new URL("http://" + IP + "/arduino/digital/13/1");

                    // Read all the text returned by the server
                    BufferedReader in = new BufferedReader(new InputStreamReader(url.openStream()));
                    in.close();
                } catch (MalformedURLException e) {
                } catch (IOException e) {
                }
                break;
            case "off":
                try {
                    // Create a URL for the desired page
                    URL url = new URL("http://" + IP + "/arduino/digital/13/0");

                    // Read all the text returned by the server
                    BufferedReader in = new BufferedReader(new InputStreamReader(url.openStream()));
                    in.close();
                } catch (MalformedURLException e) {
                } catch (IOException e) {
                }
                break;
            case "help":
                System.out.println("");
                System.out.println("on   exit");
                System.out.println("off  help");
                System.out.println("");
                break;
            case "exit":
                try {
                    // Create a URL for the desired page
                    URL url = new URL("http://" + IP + "/arduino/digital/13/0");

                    // Read all the text returned by the server
                    BufferedReader in = new BufferedReader(new InputStreamReader(url.openStream()));
                    in.close();
                } catch (MalformedURLException e) {
                } catch (IOException e) {
                }
                break OUTER;
            }
            if (fast == 1) {
                URL oracle = new URL("http://" + IP + "/arduino/digital/13");
                try (BufferedReader in = new BufferedReader(new InputStreamReader(oracle.openStream()))) {
                    String inputLine;
                    while ((inputLine = in.readLine()) != null)
                        System.out.println(inputLine);
                }
            } else {
                System.out.println("Success");
            }
        }
    }
}