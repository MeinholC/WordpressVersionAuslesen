using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Security.Policy;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            //mit dem Button Klick kann man die Form FrmMain öffnen
            FrmMain frmMain = new FrmMain();
            frmMain.ShowDialog();


        }
    }
}

