using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Internet_Connectivity_Keeper
{
    public partial class Form_PingTimeout : Form
    {
        int m_CurrentPing;

        public Form_PingTimeout(int i_CurrentPing)
        {
            m_CurrentPing = i_CurrentPing;
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            txt_Input.Text = m_CurrentPing.ToString();
            txt_Input.SelectAll();
        }

        public int GetPingTimeout()
        {
            ShowDialog();
            return m_CurrentPing;
        }

        private void btn_Accept_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txt_Input.Text, out m_CurrentPing))
            {
                MessageBox.Show("Please write a number");
                return;
            }

            Close();
        }

        private void btn_Cancle_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
