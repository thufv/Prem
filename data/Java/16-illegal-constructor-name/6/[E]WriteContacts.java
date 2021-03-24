//WriteContactsAssignment: A program to enter contact information to be written to a Contacts file
//Author: Daniel Golightly
//Date: February 26, 2014

import javax.swing.JFrame;
import javax.swing.JLabel;
import javax.swing.JPanel;
import javax.swing.JButton;
import javax.swing.*;
import java.awt.*;
import java.awt.Dialog;
import java.awt.event.*;
import java.io.*;
import java.io.FileWriter;
import java.lang.System.*;

public class WriteContacts extends JFrame implements ActionListener
{   
    //GUI COMPONENTS
    JLabel nameLabel;
    JLabel ageLabel;
    JLabel emailLabel;
    JLabel phoneLabel;
    JTextField nameField;
    JTextField ageField;
    JTextField emailField;
    JTextField phoneField;
    JButton btnWrite;
    JPanel panelWrite;

    //VARIABLES FOR WRITING
    String name;
    String email;
    String phone;
    String ageString; //holds age to be converted to integer for verifying
    int age; //variable for age after conversion to integer for verifying

    //CONSTRUCTOR FOR FILE WRITING
    public static void writeToFile(String textToWrite)
    {
        final String outputPath = "ContactsFile.txt";
        try
        {
            FileWriter contactWriter = new FileWriter(outputPath);
            contactWriter.write(textToWrite);
            contactWriter.close();
        }
        catch (IOException ex2)
        {
            JOptionPane.showMessageDialog(frame,
            "The file did not create properly",
            "Oops!", JOptionPane.ERROR_MESSAGE);
        }
    }

    //GUI CONSTRUCTOR
    public WriteContacts2()
    {
        setUndecorated(false);//GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI
        setResizable(false);
        setSize(400, 200);
        setLocationRelativeTo(null);
        setDefaultCloseOperation(EXIT_ON_CLOSE);
        setTitle("Contact Writer");

        GridLayout fieldsLayout = new GridLayout(5, 2, 10, 10);

        panelWrite = new JPanel();
        panelWrite.setLayout(fieldsLayout);

        nameLabel = new JLabel("Name: ");
        panelWrite.add(nameLabel);
        nameField = new JTextField(30);
        panelWrite.add(nameField);
        ageLabel = new JLabel("Age: ");
        panelWrite.add(ageLabel);
        ageField = new JTextField(2);
        panelWrite.add(ageField);
        phoneLabel = new JLabel("Phone Number: ");
        panelWrite.add(phoneLabel);
        phoneField = new JTextField(10);
        panelWrite.add(phoneField);
        emailLabel = new JLabel("Email: ");
        panelWrite.add(emailLabel);
        emailField = new JTextField(50);
        panelWrite.add(emailField);
        panelWrite.add(new JLabel(" "));
        btnWrite = new JButton("Submit");
        panelWrite.add(btnWrite);
        add(panelWrite);

        setVisible(true);
    }

    //VALIDATES ENTRY, WRITES TO FILE, CLEARS FIELDS FOR ADDITIONAL ENTRY
    public void actionPerformed(ActionEvent e)
    {

        name = nameField.getText();
        email = emailField.getText();
        phone = phoneField.getText();
        ageString = ageField.getText();

        try
        {
            age = Integer.parseInt(ageString);
        }
        catch (NumberFormatException ex)
        {
            JOptionPane.showMessageDialog(frame,
            "The Age you entered is not a number",
            "Age Error",
            JOptionPane.ERROR_MESSAGE);
        }

        if (0 > age || age > 120)
        {
            JOptionPane.ShowMessageDialog(frame,
            "The age you entered is invalid. Please enter between 1 and 120",
            "Age Error",
            JOptionPane.ERROR_MESSAGE);
        }

        else if (ContactsFile.exists())
        {
            writeToFile("Name: " + name + "/n");
            writeToFile("E-Mail: " + email + "/n");
            writeToFile("Age: " + age + "/n");
            writeToFile("Phone Number: " + phone + "/n" + "/n");        
            //OPEN A FILE WRITER HERE TO WRITE TO THE NEXT LINE OF THE FILE, WRITING EACH VARIABLE TO A DIFFERENT LINE
            //LEAVE TWO BLANK LINES AT THE BOTTOM OF THE FILE
            //CLOSE THE WRITER
        }

        nameField.setText("");
        emailField.setText("");
        ageField.setText("");
        phoneField.setText("");
        //WRITE MESSAGE THAT WRITE WAS SUCCESSFUL
        //CLEAR ENTRY FIELDS
        //EXIT EVENT    
    }

    //MAIN ARGS
    public static void main (String[] args)
    {
        WriteContacts2 WriteGUI = new WriteContacts2();
    }
}