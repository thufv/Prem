using System;
using System.Collections;

public class Program
{
    ArrayList lstNumbers;
    private ArrayList QuestionAnswer_Load(object sender, EventArgs e)
    {

        txtQuestion.Enabled = false;
        txtQuestion.BackColor = Color.White;
        grpMultipleChoice.Enabled = false;
        grpSingleChoice.Enabled = false;

        btnCheckAnswer.Enabled = false;
        btnNext.Enabled = false;
        btnQuit.Enabled = false;

        //force student to enter registration details
        if (txtStudentName.Text == "" && txtStudentNumber.Text == "" && txtModuleNumber.Text == "")
        {
            btnStart.Enabled = false;
            MessageBox.Show("You must enter your registration details at the upper right corner");
        }

        //declare a list
        lstNumbers = new ArrayList();

        //create a random number generator
        Random rndNumber = new Random();

        //generate 70 random numbers
        //int number = (int)(rndNumber.NextDouble() * 69) + 1;

        //lstNumbers.Add(number);
        ///use this counter to loop whenever a number is generated
        int count = 0;
        int maximumNumber = 69;

        ///disable answer button 
        btnCheckAnswer.Enabled = false;
        do
        {

            int number = (int)(rndNumber.NextDouble() * maximumNumber) + 1;
            lstNumbers.Add(number);
            if (!lstNumbers.Contains(number))
            {
                lstNumbers.Add(number);
            }
            count++;

        } while (count <= 15 * 70);//
        btnCheckAnswer_Click(sender, e);
        return lstNumbers;//returns once list is built
    }
}
