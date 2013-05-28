using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;

namespace MJr_SQLServer2PostgreSQL
{
    public partial class TelaWait : DevExpress.XtraEditors.XtraForm
    {
        public TelaWait()
        {
            InitializeComponent();
        }

        public void Progress(string caption, string description)
        {
            Application.DoEvents();
            pbWait.Caption = caption;
            pbWait.Description = description;
            pbWait.Refresh();
            this.Refresh();
            this.Show();            
        }
    }
}