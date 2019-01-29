using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Freya
{
    public partial class FormWait : Form
    {
        private readonly MethodInvoker method;

        public FormWait(MethodInvoker action)
        {
            InitializeComponent();

            //設定label自動換行
            label_FormWaitMessage.AutoSize = true;
            label_FormWaitMessage.Dock = DockStyle.Fill;
            label_FormWaitMessage.MaximumSize = new System.Drawing.Size(150, 0);


            method = action;
        }

        private void WaitForm_Load(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                method.Invoke();
                InvokeAction(this, Dispose);
            }).Start();
        }

        public static void InvokeAction(Control control, MethodInvoker action)
        {
            if (control.InvokeRequired)
            {
                control.BeginInvoke(action);
            }
            else
            {
                action();
            }
        }

        public FormWait SetMessage(string s)
        {
            this.label_FormWaitMessage.Text = s;
            return this;
        }
    }
}
