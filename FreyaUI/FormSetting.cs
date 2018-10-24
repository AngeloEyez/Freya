using Freya.Proxy;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.XPath;
using ZetaIpc.Runtime.Client;
using Newtonsoft.Json;
using System.Reflection;

namespace Freya
{
    public partial class FormSetting : Form
    {
        public IpcClient radioClient;
        private FRegSetting reg = new FRegSetting();

        public FormSetting()
        {
            InitializeComponent();
            Options_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;//設定button1為OK
            Options_OK.DialogResult = System.Windows.Forms.DialogResult.OK;//設定button為Cancel
        }

        private void FormSetting_Load(object sender, EventArgs e)
        {
            //// Advanced - LogLevel
            comboBox_LogLevel.DataSource = Enum.GetValues(typeof(FConstants.FreyaLogLevel));
            var loglevel = FFunc.GetRegKey("LogLevel");
            comboBox_LogLevel.SelectedItem = (loglevel == null) ? FConstants.FreyaLogLevel.Normal : (FConstants.FreyaLogLevel)loglevel;

            //// Mail - Email Addres
            textBox_Email.Text = (string)FFunc.GetRegKey("EMail");
            if (textBox_Email.Text.Length == 0)
                Options_OK.Enabled = false;

            //// Mail - SMTP Server
            textBox_SMTPServer.Text = (string)FFunc.GetRegKey("SmtpServerIp");
            if (textBox_SMTPServer.Text.Length == 0)
                Options_OK.Enabled = false;

            //// Mail - WebService
            textBox_WebService.Text = (string)FFunc.GetRegKey("WebService");
            if (textBox_WebService.Text.Length == 0)
                Options_OK.Enabled = false;

            //// Mail - SMTPLogLevel
            string[] SMTPLogLevels = {"None", "Critical", "Error", "Warning", "Information", "Verbose", "Raw" };
            comboBox_SMTPLogLevel.Items.AddRange(SMTPLogLevels);
            string SMTPLogLevel = (string)FFunc.GetRegKey("SMTPLogLevel");
            comboBox_SMTPLogLevel.SelectedItem = (SMTPLogLevel == null) ? SMTPLogLevels[0] : SMTPLogLevel;

            //// 確認哪些UI要顯示
            reg.FeatureByte = (FConstants.FeatureByte)Convert.ToInt32(FFunc.GetRegKey("FeatureByte"));
            //if ((reg.FeatureByte & FConstants.FeatureByte.FullUI) == FConstants.FeatureByte.FullUI)
            if (FFunc.HasRight(FConstants.FeatureByte.FullUI))  //不能從registry來判斷，在OK前都要看reg.FeatureByte
                this.tabPage2.Parent = this.tabControl1;
            else
                this.tabPage2.Parent = null;

            this.tabPage3.Parent = ((reg.FeatureByte & FConstants.FeatureByte.Dark) == FConstants.FeatureByte.Dark) ? null : this.tabControl1;



        }

        private void Options_OK_Click(object sender, EventArgs e)
        {
            // Dark mode某些sheet隱藏起來讀取會有問題，直接跳過不更新設定
            //if ((reg.FeatureByte & FConstants.FeatureByte.Dark) == FConstants.FeatureByte.Dark)
            //    return;

            reg.LogLevel = (FConstants.FreyaLogLevel)comboBox_LogLevel.SelectedItem;

            if (textBox_Email.Text.Length > 0)
                reg.EMail = textBox_Email.Text;

            if (textBox_SMTPServer.Text.Length > 0)
                reg.SMTPServerIP = textBox_SMTPServer.Text;

            if (textBox_WebService.Text.Length > 0)
                reg.WebServiceIP = textBox_WebService.Text;

            reg.SMTPLogLevel = (string)comboBox_SMTPLogLevel.SelectedItem;

            string RegJSON = JsonConvert.SerializeObject(reg);
            radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "WriteRegistry", Data2 = RegJSON }));
        }

        private void textBox_FeatureByte_TextChanged(object sender, EventArgs e)
        {
            switch (textBox_FeatureByte.Text)
            {
                case "FullUI":
                    if ((reg.FeatureByte & FConstants.FeatureByte.FullUI) == FConstants.FeatureByte.FullUI)
                    {
                        reg.FeatureByte = (reg.FeatureByte & (FConstants.FeatureByte.ALL ^ FConstants.FeatureByte.FullUI)); //刪除
                        this.tabPage2.Parent = null; //隱藏 tab
                    }
                    else
                    {
                        reg.FeatureByte = (reg.FeatureByte | FConstants.FeatureByte.FullUI); //增加
                        this.tabPage2.Parent = this.tabControl1; //顯示tab
                    }
                    textBox_FeatureByte.Text = "";

                    break;
                case "SMTPProxy":
                    if ((reg.FeatureByte & FConstants.FeatureByte.SMTPProxy) == FConstants.FeatureByte.SMTPProxy)
                        reg.FeatureByte = (reg.FeatureByte & (FConstants.FeatureByte.ALL ^ FConstants.FeatureByte.SMTPProxy)); //刪除
                    else
                        reg.FeatureByte = (reg.FeatureByte | FConstants.FeatureByte.SMTPProxy); //增加
                    textBox_FeatureByte.Text = "";
                    break;
                case "DMS":
                    if ((reg.FeatureByte & FConstants.FeatureByte.DMS) == FConstants.FeatureByte.DMS)
                        reg.FeatureByte = (reg.FeatureByte & (FConstants.FeatureByte.ALL ^ FConstants.FeatureByte.DMS)); //刪除
                    else
                        reg.FeatureByte = (reg.FeatureByte | FConstants.FeatureByte.DMS); //增加
                    textBox_FeatureByte.Text = "";
                    break;
                case "Dark":
                    if ((reg.FeatureByte & FConstants.FeatureByte.Dark) == FConstants.FeatureByte.Dark)
                    {
                        reg.FeatureByte = (reg.FeatureByte & (FConstants.FeatureByte.ALL ^ FConstants.FeatureByte.Dark)); //刪除
                        this.tabPage3.Parent = this.tabControl1;
                    }
                    else
                    {
                        reg.FeatureByte = (reg.FeatureByte | FConstants.FeatureByte.Dark); //增加
                        this.tabPage3.Parent = null;
                        Options_OK.Enabled = true;
                    }
                    textBox_FeatureByte.Text = "";
                    break;

                case "Reset":
                    FFunc.SetRegKey("FeatureByte", Convert.ToString((int)136, 16)); // default: 1000_1000
                    textBox_FeatureByte.Text = "";
                    break;
                default:
                    break;
            }
        }

        private void textBox_Email_TextChanged(object sender, EventArgs e)
        {
            if (textBox_Email.Text.Length > 0 && textBox_SMTPServer.Text.Length > 0 && textBox_WebService.Text.Length > 0)
                Options_OK.Enabled = true;
            else
                Options_OK.Enabled = false;
        }

        private void textBox_SMTPServer_TextChanged(object sender, EventArgs e)
        {
            if (textBox_Email.Text.Length > 0 && textBox_SMTPServer.Text.Length > 0 && textBox_WebService.Text.Length > 0)
                Options_OK.Enabled = true;
            else
                Options_OK.Enabled = false;
        }

        private void textBox_WebService_TextChanged(object sender, EventArgs e)
        {
            if (textBox_Email.Text.Length > 0 && textBox_SMTPServer.Text.Length > 0 && textBox_WebService.Text.Length > 0)
                Options_OK.Enabled = true;
            else
                Options_OK.Enabled = false;
        }

        private void comboBox_LogLevel_ParentChanged(object sender, EventArgs e)
        {
            var loglevel = FFunc.GetRegKey("LogLevel");
            comboBox_LogLevel.SelectedItem = (loglevel == null) ? FConstants.FreyaLogLevel.Normal : (FConstants.FreyaLogLevel)loglevel;
        }
    }
}
