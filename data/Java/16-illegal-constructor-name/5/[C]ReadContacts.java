//ReadContactsAssignment: A program to read contact information from a Contacts file
//Author: Daniel Golightly
//Date: February 26, 2014

import javax.swing.*;
import javax.swing.JFrame;
import javax.swing.JLabel;
import javax.swing.JPanel;
import javax.swing.JButton;
import javax.swing.text.*;
import java.awt.*;
import java.awt.event.*;
import java.io.*;
import java.lang.System.*;

public class ReadContacts extends JFrame implements ActionListener
{   
    //GUI COMPONENTS
    JLabel contactInfo;
    JTextArea displayArea;
    JButton btnRead;
    JPanel panelRead;

    //GUI CONSTRUCTOR
    public ReadContacts ()
    {
        setUndecorated(false);      //GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI GUI
        setSize(450, 600);
        setLocationRelativeTo(null);
        setDefaultCloseOperation(EXIT_ON_CLOSE);
        setTitle("Contact List");

        GridLayout readLayout = new GridLayout(2, 1);

        panelRead = new JPanel();
        panelRead.setLayout(readLayout);

        displayArea = new JTextArea();
        panelRead.add(displayArea);
        btnRead = new JButton("Read Contacts");
        panelRead.add(btnRead); 

        add(panelRead);

        setVisible(true);
    }

    //READS THE CONTACTS.TXT FILE TO THE TEXTAREA
    public void actionPerformed(ActionEvent e)
    {
        File ContactsFile = File("Week3Contacts.txt");
        ContactsFile.createNewFile("Week3Contacts.txt", false).close();

        try //OR SOMETHING VERY SIMILAR
        {
            if(ContactsFile.exists())
            {
                FileReader reader = new FileReader( "Week3Contacts.txt" );
                BufferedReader br = new BufferedReader(reader);
                edit.read( br, null );
                br.close();
                edit.requestFocus();
            }
        }
        catch (FileNotFoundException ex)
        {
        JOptionPane.showMessageDialog(frame,
        "The file could not be found or is corrupted. Contact your IT administrator.",
        "Oops", JOptionPane.ERROR_MESSAGE);
        }
        //VALIDATE THAT THE FILE EXISTS
        //IF FILE VALIDATION FAILS, DISPLAY FILE NOT FOUND, CONTACT ADMIN MESSAGE
        //OTHERWISE CONTINUE EVENT
        //OPEN READER AND READ TO THE TEXTAREA
        //CLOSE THE READER TO KEEP IT FROM EATING RESOURCES AND ALLOW THE FILE TO BE INTERACTED WITH
        //EXIT EVENT    
    }

    //MAIN ARGS
    public static void main (String[] args)
    {
        ReadContacts2 ReadGUI = new ReadContacts2();
    }
}