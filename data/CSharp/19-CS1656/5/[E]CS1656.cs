using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace WinFormButtonExamOl
{
    public partial class Forml : Form
    {
        public Forml()
        {
            InitializeComponent();
        }
    
        private void buttonl_Click_l(object sender, EventArgs e)
        {
            MessageBox.Show = "Buttonl is Clicked!";
            buttonl.BackColor = Color.Cyan;
            buttonl.ForeColor = Color.Blue;
        }
    }
}