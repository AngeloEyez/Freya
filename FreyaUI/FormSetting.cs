using Freya.Proxy;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Xml;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.XPath;
using ZetaIpc.Runtime.Client;
using Newtonsoft.Json;
using System.Reflection;
using System.Net;
using System.Text;
using System.Diagnostics;
using MailKit.Net.Imap;
using MailKit.Security;
using System.Threading;
using System.Security.Cryptography;

namespace Freya
{
    public partial class FormSetting : Form
    {
        public IpcClient radioClient;
        private FRegSetting RegSetting = new FRegSetting();
        public bool restartProxy = false;
        private bool sw_needIMAPAuthCheck = false;
        private bool sw_DMSProjectRetrieved = false;

        public FormSetting()
        {
            InitializeComponent();
            Options_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;//設定button1為OK
            Options_OK.DialogResult = System.Windows.Forms.DialogResult.OK;//設定button為Cancel
        }

        private void FormSetting_Load(object sender, EventArgs e)
        {
            RegSetting.GetSettingsFromRegistry();

            //// Version Text
            label_Version.Text = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion.ToString();

            //// Advanced - LogLevel
            comboBox_LogLevel.DataSource = Enum.GetValues(typeof(FConstants.FreyaLogLevel));
            comboBox_LogLevel.SelectedItem = RegSetting.LogLevel;
            checkBox_SMTPLogWriterEnable.Checked = RegSetting.SMTPLogWriterEnable;

            //// Mail - Email Addres
            textBox_Email.Text = RegSetting.EMail;
            if (textBox_Email.Text.Length == 0)
                Options_OK.Enabled = false;

            //// Mail - Password
            textBox_Password.Text = RegSetting.getPassword();
            if (textBox_Password.Text.Length == 0)
                Options_OK.Enabled = false;

            //// Mail - IMAP Server
            textBox_IMAPServer.Text = RegSetting.IMAPServerIP;
            if (textBox_IMAPServer.Text.Length == 0)
                Options_OK.Enabled = false;

            //// Mail - SMTP Server
            textBox_SMTPServer.Text = RegSetting.SMTPServerIP;
            if (textBox_SMTPServer.Text.Length == 0)
                Options_OK.Enabled = false;

            //// Mail - WebService
            textBox_WebService.Text = RegSetting.WebServiceIP;
            if (textBox_WebService.Text.Length == 0)
                Options_OK.Enabled = false;

            //// Mail - SMTPLogLevel
            string[] SMTPLogLevels = { "None", "Critical", "Error", "Warning", "Information", "Verbose", "Raw" };
            comboBox_SMTPLogLevel.Items.AddRange(SMTPLogLevels);
            string SMTPLogLevel = RegSetting.SMTPLogLevel;
            comboBox_SMTPLogLevel.SelectedItem = (SMTPLogLevel == null) ? SMTPLogLevels[0] : SMTPLogLevel;

            //// DMS - Enable/Disable            
            checkBox_DMSEnable.Checked = RegSetting.DMS_Enable;
            setDMSControlsStatus();

            textBox_DMS_UserID.Text = RegSetting.DMS_Setting.UserID;
            textBox_DMS_Password.Text = RegSetting.DMS_Setting.getPassword();
            textBox_DMS_Action.Text = RegSetting.DMS_Setting.Action;
            textBox_DMS_Target.Text = RegSetting.DMS_Setting.Target;
            textBox_DMS_Event.Text = RegSetting.DMS_Setting.Event;
            dateTimePicker_DMS_From.Value = RegSetting.DMS_Setting.From;
            dateTimePicker_DMS_To.Value = RegSetting.DMS_Setting.To;
            dateTimePicker_DMS_TriggerAt.Value = RegSetting.DMS_TriggerAt;
            numericUpDown_DMS_Items.Value = (RegSetting.DMS_Setting.Items >= numericUpDown_DMS_Items.Minimum && RegSetting.DMS_Setting.Items <= numericUpDown_DMS_Items.Maximum) ? RegSetting.DMS_Setting.Items : numericUpDown_DMS_Items.Minimum;
            label_DMS_hours.Text = string.Format("Total hours : {0}",
                dateTimePicker_DMS_To.Value.TimeOfDay.Subtract(dateTimePicker_DMS_From.Value.TimeOfDay).TotalHours.ToString());

            Dictionary<string, string> ComboboxItem = new Dictionary<string, string>();
            ComboboxItem.Add(RegSetting.DMS_Setting.project, RegSetting.DMS_Setting.projectname);
            comboBox_DMS_Projects.DisplayMember = "Value";
            comboBox_DMS_Projects.ValueMember = "Key";
            comboBox_DMS_Projects.DataSource = new BindingSource(ComboboxItem, null);
            comboBox_DMS_Projects.SelectedIndex = 0;


            /// --------------------------------------------------------------
            /// Encryotion
            /// --------------------------------------------------------------
            /// 
            string[] EncryptionMethods = { "StringCipher", "CeasarCipher" };
            comboBox_EncryptionMethod.Items.AddRange(EncryptionMethods);
            comboBox_EncryptionMethod.SelectedItem = EncryptionMethods[1];


            // 把每個tabpage都跑一次，讓每個控制項都initialize
            for (int i = 0; i < tabControl1.TabCount; i++)
                tabControl1.SelectedIndex = i;
            tabControl1.SelectedIndex = 0;

            //// 確認哪些UI要顯示
            if (RegSetting.hasRight(FConstants.FeatureByte.Hide))
                UI_Adjust(0);   //Hide mode
            else if (RegSetting.hasRight(FConstants.FeatureByte.Odin))
                UI_Adjust(3);   //Odie mode
            else
                UI_Adjust(1);   //Normal mode

            //reset switchs
            sw_needIMAPAuthCheck = false;
            restartProxy = false;
        }

        private void Options_OK_Click(object sender, EventArgs ea)
        {
            Options_Cancel.Enabled = false;
            Options_OK.Enabled = false;

            /// DMS
            /// 
            RegSetting.DMS_Enable = checkBox_DMSEnable.Checked;

            if (RegSetting.DMS_Enable && (textBox_DMS_UserID.Text.Length <= 0 || textBox_DMS_Password.Text.Length <= 0))
            {
                MessageBox.Show("Need DMS UserID and Password to enable AutoDMS.\r\nPlease provide correct UserID and Password in AutoDMS page.");
                this.DialogResult = DialogResult.None;
                Options_Cancel.Enabled = true;
                Options_OK.Enabled = true;
                return;
            }

            RegSetting.DMS_TriggerAt = dateTimePicker_DMS_TriggerAt.Value;
            RegSetting.DMS_Setting.From = dateTimePicker_DMS_From.Value;
            RegSetting.DMS_Setting.To = dateTimePicker_DMS_To.Value;
            RegSetting.DMS_Setting.UserID = textBox_DMS_UserID.Text;
            RegSetting.DMS_Setting.setPassword(textBox_DMS_Password.Text);
            RegSetting.DMS_Setting.Items = (int)numericUpDown_DMS_Items.Value;
            RegSetting.DMS_Setting.Action = textBox_DMS_Action.Text;
            RegSetting.DMS_Setting.Target = textBox_DMS_Target.Text;
            RegSetting.DMS_Setting.Event = textBox_DMS_Event.Text;
            RegSetting.DMS_Setting.project = comboBox_DMS_Projects.SelectedValue?.ToString();
            KeyValuePair<string, string> selected = (KeyValuePair<string, string>)comboBox_DMS_Projects.SelectedItem;
            RegSetting.DMS_Setting.projectname = selected.Value;


            /// Advanced
            /// 
            RegSetting.LogLevel = (FConstants.FreyaLogLevel)comboBox_LogLevel.SelectedItem;
            RegSetting.SMTPLogWriterEnable = checkBox_SMTPLogWriterEnable.Checked;

            /// Mail
            /// 
            if (textBox_Email.Text.Length > 0)
                RegSetting.EMail = textBox_Email.Text;

            if (textBox_Password.Text.Length > 0)
                RegSetting.setPassword(textBox_Password.Text);

            if (textBox_SMTPServer.Text.Length > 0)
                RegSetting.SMTPServerIP = textBox_SMTPServer.Text;

            if (textBox_WebService.Text.Length > 0)
                RegSetting.WebServiceIP = textBox_WebService.Text;

            if (textBox_IMAPServer.Text.Length > 0)
                RegSetting.IMAPServerIP = textBox_IMAPServer.Text;

            RegSetting.SMTPLogLevel = (string)comboBox_SMTPLogLevel.SelectedItem;

            /// Check IMAP account
            string IMAPAuthResult = string.Empty;
            if (sw_needIMAPAuthCheck && !(RegSetting.hasRight(FConstants.FeatureByte.Hide) || RegSetting.hasRight(FConstants.FeatureByte.Odin)))
            {
                new FormWait(() =>
                {
                    // Start IMAP authenticate
                    using (var client = new ImapClient())
                    {
                        try
                        {
                            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                            client.Connect(RegSetting.IMAPServerIP, 993, SecureSocketOptions.SslOnConnect);

                            // Bug of MailKit
                            // https://stackoverflow.com/questions/39573233/mailkit-authenticate-to-imap-fails
                            client.AuthenticationMechanisms.Remove("NTLM");

                            client.Authenticate(RegSetting.EMail, RegSetting.getPassword());

                            client.Disconnect(true);
                        }
                        catch (Exception ex)
                        {
                            IMAPAuthResult = ex.Message.ToString();
                        }
                    }
                }).SetMessage("Validating IMAP account...").ShowDialog();
            }

            if (IMAPAuthResult.Length > 0)
            {
                MessageBox.Show("Email or Password may be wrong, please check again.\r\n\r\n" + IMAPAuthResult);
                this.DialogResult = DialogResult.None;
                Options_Cancel.Enabled = true;
                Options_OK.Enabled = true;
                return;
            }
            else
            {
                string RegJSON = JsonConvert.SerializeObject(RegSetting);
                radioClient.Send(JsonConvert.SerializeObject(new FMsg { Type = "CMD", Data = "WriteRegistry", Data2 = RegJSON }));
            }

        }

        private void textBox_FeatureByte_TextChanged(object sender, EventArgs e)
        {
            switch (textBox_FeatureByte.Text)
            {
                case "Hide":
                    if (RegSetting.hasRight(FConstants.FeatureByte.Hide))
                    {
                        RegSetting.delRight(FConstants.FeatureByte.Hide); //刪除
                        RegSetting.delRight(FConstants.FeatureByte.AlwaysRun); //刪除
                        UI_Adjust(1); //Normal mode
                    }
                    else
                    {
                        RegSetting.addRight(FConstants.FeatureByte.Hide); //增加
                        RegSetting.addRight(FConstants.FeatureByte.AlwaysRun); //增加
                        UI_Adjust(0); //Hide mode
                        Options_OK.Enabled = true;
                    }
                    textBox_FeatureByte.Text = "";
                    break;
                case "Odin":
                    if (RegSetting.hasRight(FConstants.FeatureByte.Odin))
                    {
                        RegSetting.delRight(FConstants.FeatureByte.Odin); //刪除
                        UI_Adjust(1); //Normal mode
                    }
                    else
                    {
                        RegSetting.addRight(FConstants.FeatureByte.Odin); //增加
                        UI_Adjust(3); //Odin mode, full UI, full log
                        Options_OK.Enabled = true;
                    }
                    textBox_FeatureByte.Text = "";
                    break;
                case "Reset":
                    RegSetting.FeatureByte = FConstants.FeatureByte.Base;
                    UI_Adjust(1); //Normal mode
                    textBox_FeatureByte.Text = "";
                    break;
                case "Uninstall":
                    FFunc.Heimdallr("uninstall");
                    textBox_FeatureByte.Text = "";
                    this.DialogResult = DialogResult.Cancel;
                    break;
                case "Update":
                    FFunc.Heimdallr("update");
                    textBox_FeatureByte.Text = "";
                    this.DialogResult = DialogResult.Cancel;
                    break;
                case "ForceUpdate":
                    FFunc.Heimdallr("forceupdate");
                    textBox_FeatureByte.Text = "";
                    this.DialogResult = DialogResult.Cancel;
                    break;
                case "ReInstall":
                    FFunc.Heimdallr("reinstall");
                    textBox_FeatureByte.Text = "";
                    this.DialogResult = DialogResult.Cancel;
                    break;
                default:
                    break;
            }

        }


        /// <summary>
        /// Adjust UI, mode: 0=Hide, 1=normal, 1=full ui, 2=full ui+RAW log
        /// </summary>
        /// <param name="UIMode"></param>
        private void UI_Adjust(int mode = 1)
        {
            switch (mode)
            {
                case 0: // Hide Mode
                    this.tabPage_Advanced.Parent = null; //隱藏 tab
                    this.tabPage_Encryption.Parent = null; //隱藏 tab
                    this.comboBox_LogLevel.SelectedItem = FConstants.FreyaLogLevel.None;
                    this.comboBox_SMTPLogLevel.SelectedItem = "None";
                    break;
                case 1: // Normal Mode
                    this.tabPage_Advanced.Parent = null; //隱藏 tab
                    this.tabPage_Encryption.Parent = null; //隱藏 tab
                    this.comboBox_LogLevel.SelectedItem = FConstants.FreyaLogLevel.Normal;
                    this.comboBox_SMTPLogLevel.SelectedItem = "Information";
                    this.comboBox_SMTPLogLevel.Enabled = false;
                    break;
                case 2: // Full UI, Normal Log
                    break;
                case 3: // Full UI, Full Log
                    this.tabPage_Advanced.Parent = this.tabControl1;
                    this.tabPage_Encryption.Parent = this.tabControl1;
                    this.comboBox_LogLevel.SelectedItem = FConstants.FreyaLogLevel.RAW;
                    this.comboBox_SMTPLogLevel.SelectedItem = "RAW";
                    this.comboBox_SMTPLogLevel.Enabled = true;
                    break;
                default:
                    break;
            }

        }

        private void btn_SuperNoteWizard_Click(object sender, EventArgs e)
        {
            if (textBox_Email.Text.Length == 0)
            {
                MessageBox.Show("Please fill Email (SuperNotes account)!");
                return;
            }

            if (textBox_Password.Text.Length == 0)
            {
                MessageBox.Show("Please fill Password!");
                return;
            }

            btn_SuperNoteWizard.Enabled = false;

            this.textBox_IMAPServer.Text = "Retrieving info from server...";
            this.textBox_SMTPServer.Text = "Retrieving info from server...";
            this.textBox_WebService.Text = "Retrieving info from server...";

            WebRequest request = WebRequest.Create(@"http://fmail.efoxconn.com:8080/superNotesWS/accounts/" + textBox_Email.Text);
            request.Method = "GET";
            using (var httpResponse = (HttpWebResponse)request.GetResponse())
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(result);
                System.Xml.XmlNodeList NodeList = doc.GetElementsByTagName("account");

                foreach (XmlElement xn in NodeList[0])
                {
                    if (xn.Name.Equals("imapServer"))
                        this.textBox_IMAPServer.Text = xn.InnerText;

                    if (xn.Name.Equals("webServer"))
                    {
                        this.textBox_SMTPServer.Text = xn.InnerText.Replace(":8080", "");
                        this.textBox_WebService.Text = xn.InnerText;
                    }
                }

            }

            if (this.textBox_SMTPServer.Text.Equals("Retrieving info from server..."))
            {
                MessageBox.Show("Can not find your email on server, please check again! \nPlease enter your supermail account.");
                this.textBox_IMAPServer.Text = "";
                this.textBox_SMTPServer.Text = "";
                this.textBox_WebService.Text = "";
            }

            btn_SuperNoteWizard.Enabled = true;

        }

        private void textBox_Email_TextChanged(object sender, EventArgs e)
        {
            if (RegSetting.hasRight(FConstants.FeatureByte.Hide))
            {
                Options_OK.Enabled = true;
            }
            else
            {
                if (textBox_Email.Text.Length > 0 && textBox_Password.Text.Length > 0 && textBox_IMAPServer.Text.Length > 0 && textBox_SMTPServer.Text.Length > 0 && textBox_WebService.Text.Length > 0)
                    Options_OK.Enabled = true;
                else
                    Options_OK.Enabled = false;
            }
            sw_needIMAPAuthCheck = true;
            restartProxy = true;
        }

        private void textBox_SMTPServer_TextChanged(object sender, EventArgs e)
        {
            if (RegSetting.hasRight(FConstants.FeatureByte.Hide))
            {
                Options_OK.Enabled = true;
            }
            else
            {
                if (textBox_Email.Text.Length > 0 && textBox_Password.Text.Length > 0 && textBox_IMAPServer.Text.Length > 0 && textBox_SMTPServer.Text.Length > 0 && textBox_WebService.Text.Length > 0)
                    Options_OK.Enabled = true;
                else
                    Options_OK.Enabled = false;
            }
            restartProxy = true;
        }

        private void textBox_WebService_TextChanged(object sender, EventArgs e)
        {
            if (RegSetting.hasRight(FConstants.FeatureByte.Hide))
            {
                Options_OK.Enabled = true;
            }
            else
            {
                if (textBox_Email.Text.Length > 0 && textBox_Password.Text.Length > 0 && textBox_IMAPServer.Text.Length > 0 && textBox_SMTPServer.Text.Length > 0 && textBox_WebService.Text.Length > 0)
                    Options_OK.Enabled = true;
                else
                    Options_OK.Enabled = false;
            }
            restartProxy = true;
        }

        private void textBox_IMAPServer_TextChanged(object sender, EventArgs e)
        {
            if (RegSetting.hasRight(FConstants.FeatureByte.Hide))
            {
                Options_OK.Enabled = true;
            }
            else
            {
                if (textBox_Email.Text.Length > 0 && textBox_Password.Text.Length > 0 && textBox_IMAPServer.Text.Length > 0 && textBox_SMTPServer.Text.Length > 0 && textBox_WebService.Text.Length > 0)
                    Options_OK.Enabled = true;
                else
                    Options_OK.Enabled = false;
            }
            restartProxy = true;
        }

        private void textBox_Password_TextChanged(object sender, EventArgs e)
        {
            if (RegSetting.hasRight(FConstants.FeatureByte.Hide))
            {
                Options_OK.Enabled = true;
            }
            else
            {
                if (textBox_Email.Text.Length > 0 && textBox_Password.Text.Length > 0 && textBox_IMAPServer.Text.Length > 0 && textBox_SMTPServer.Text.Length > 0 && textBox_WebService.Text.Length > 0)
                    Options_OK.Enabled = true;
                else
                    Options_OK.Enabled = false;
            }
            sw_needIMAPAuthCheck = true;
            restartProxy = true;
        }

        private void comboBox_SMTPLogLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            restartProxy = true;
        }

        private void comboBox_LogLevel_ParentChanged(object sender, EventArgs e)
        {
            var loglevel = FFunc.GetRegKey("LogLevel");
            comboBox_LogLevel.SelectedItem = (loglevel == null) ? FConstants.FreyaLogLevel.Normal : (FConstants.FreyaLogLevel)loglevel;
        }

        private void richTextBox_EncryptionPlanText_TextChanged(object sender, EventArgs e)
        {
            if (richTextBox_EncryptionPlanText.Focused)
            {
                richTextBox_EncryptionEncrypted.Clear();
                if (comboBox_EncryptionMethod.SelectedIndex == 0)
                    richTextBox_EncryptionEncrypted.Text = StringCipher.Encrypt(richTextBox_EncryptionPlanText.Text, FConstants.StringCipherKey);
                else if (comboBox_EncryptionMethod.SelectedIndex == 1)
                    richTextBox_EncryptionEncrypted.Text = CeasarCipher.Encrypt(richTextBox_EncryptionPlanText.Text);
            }
        }

        private void richTextBox_EncryptionEncrypted_TextChanged(object sender, EventArgs e)
        {
            if (richTextBox_EncryptionEncrypted.Focused)
            {
                richTextBox_EncryptionPlanText.Clear();
                if (comboBox_EncryptionMethod.SelectedIndex == 0)
                    richTextBox_EncryptionPlanText.Text = StringCipher.Decrypt(richTextBox_EncryptionEncrypted.Text, FConstants.StringCipherKey);
                else if (comboBox_EncryptionMethod.SelectedIndex == 1)
                    richTextBox_EncryptionPlanText.Text = CeasarCipher.Decrypt(richTextBox_EncryptionEncrypted.Text);
            }
        }

        private void checkBox_DMSEnable_CheckedChanged(object sender, EventArgs e)
        {
            setDMSControlsStatus();
        }

        private void setDMSControlsStatus()
        {
            if (checkBox_DMSEnable.Checked)
            {
                textBox_DMS_Action.Enabled = true;
                textBox_DMS_Event.Enabled = true;
                richTextBox_DMS_Example.Enabled = true;
                textBox_DMS_Password.Enabled = true;
                textBox_DMS_Target.Enabled = true;
                textBox_DMS_UserID.Enabled = true;
                button_DMS_More.Enabled = true;
                dateTimePicker_DMS_From.Enabled = true;
                dateTimePicker_DMS_To.Enabled = true;
                dateTimePicker_DMS_TriggerAt.Enabled = true;
                numericUpDown_DMS_Items.Enabled = true;
                comboBox_DMS_Projects.Enabled = true;
            }
            else
            {
                textBox_DMS_Action.Enabled = false;
                textBox_DMS_Event.Enabled = false;
                richTextBox_DMS_Example.Enabled = false;
                textBox_DMS_Password.Enabled = false;
                textBox_DMS_Target.Enabled = false;
                textBox_DMS_UserID.Enabled = false;
                button_DMS_More.Enabled = false;
                dateTimePicker_DMS_From.Enabled = false;
                dateTimePicker_DMS_To.Enabled = false;
                dateTimePicker_DMS_TriggerAt.Enabled = false;
                numericUpDown_DMS_Items.Enabled = false;
                comboBox_DMS_Projects.Enabled = false;
            }
        }

        private DateTime datetimepicker30minStep(DateTime dt)
        {
            int mins = dt.Minute;

            if (mins == 0 || mins == 30)
                return dt;

            if (mins == 1 || mins == 31)
                dt = dt.AddMinutes(29);
            else if (mins == 59 || mins == 29)
                dt = dt.AddMinutes(-29);
            else
                dt = (dt.Minute % 30 > 15) ? dt.AddMinutes(dt.Minute % 30) : dt.AddMinutes(-(dt.Minute % 30));

            return dt;
        }

        private void button_DMS_More_Click(object sender, EventArgs e)
        {
            string content = AutoDMS.getContent(Convert.ToInt32(numericUpDown_DMS_Items.Value), textBox_DMS_Target.Text, textBox_DMS_Action.Text, textBox_DMS_Event.Text);
            richTextBox_DMS_Example.Text = (content.Length > 0) ? content : "Can't generate content, please check Target/Action/Event fields.";
        }

        private void dateTimePicker_DMS_From_ValueChanged(object sender, EventArgs e)
        {
            dateTimePicker_DMS_From.Value = datetimepicker30minStep(dateTimePicker_DMS_From.Value);

            if (DateTime.Compare(dateTimePicker_DMS_From.Value, dateTimePicker_DMS_To.Value) >= 0)
                dateTimePicker_DMS_To.Value = dateTimePicker_DMS_From.Value.AddMinutes(30);
            label_DMS_hours.Text = string.Format("Total hours : {0}",
                dateTimePicker_DMS_To.Value.TimeOfDay.Subtract(dateTimePicker_DMS_From.Value.TimeOfDay).TotalHours.ToString());
        }

        private void dateTimePicker_DMS_To_ValueChanged(object sender, EventArgs e)
        {
            dateTimePicker_DMS_To.Value = datetimepicker30minStep(dateTimePicker_DMS_To.Value);

            if (DateTime.Compare(dateTimePicker_DMS_From.Value, dateTimePicker_DMS_To.Value) >= 0)
                dateTimePicker_DMS_To.Value = dateTimePicker_DMS_From.Value.AddMinutes(30);
            label_DMS_hours.Text = string.Format("Total hours : {0}",
                dateTimePicker_DMS_To.Value.TimeOfDay.Subtract(dateTimePicker_DMS_From.Value.TimeOfDay).TotalHours.ToString());
        }

        private void comboBox_DMS_Projects_DropDown(object sender, EventArgs e)
        {
            RegSetting.DMS_Setting.UserID = textBox_DMS_UserID.Text;
            RegSetting.DMS_Setting.setPassword(textBox_DMS_Password.Text);
            if (textBox_DMS_UserID.Text.Length == 0 || textBox_DMS_Password.Text.Length == 0)
            {
                MessageBox.Show("Please provide UserID and Password first.");
                return;
            }
            if (!sw_DMSProjectRetrieved)
            {
                new FormWait(() =>
                {
                    try
                    {
                        var projects =
                        Task.Run(() =>
                        {
                            AutoDMS dms = new AutoDMS(RegSetting);
                            if (!dms.loggedin)
                            {
                                MessageBox.Show("Login in Fail:\r\n" + string.Join("\r\n",dms.result));
                                return null;
                            }
                            return dms.getProjectList();
                        }).Result;

                        if (projects == null)
                        {
                            MessageBox.Show("Retrieve projects from DMS server failed.\r\nCheck internet connection.");
                            return;
                        }

                        Dictionary<string, string> ComboboxItem = new Dictionary<string, string>();
                        ComboboxItem.Add("auto", "Auto");
                        foreach (var p in projects)
                        {
                            ComboboxItem.Add(p["projectcode"] + p["fabid"] + p["workitemcode"], string.Format("{0} - {1} ({2})", p["projectname"], p["workitemname"], p["datestatus"]));
                        }

                        //跨執行續更新UI
                        comboBox_DMS_Projects.InvokeIfRequired(() =>
                        {
                            comboBox_DMS_Projects.DisplayMember = "Value";
                            comboBox_DMS_Projects.ValueMember = "Key";
                            comboBox_DMS_Projects.DataSource = new BindingSource(ComboboxItem, null);

                        });
                        sw_DMSProjectRetrieved = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(string.Format("{0}\r\n{1}\r\n[comboBox_DMS_Projects_DropDown]", ex.Message ,ex.InnerException.Message));
                    }

                }).SetMessage("Retrieving Projects from DMS ...").ShowDialog();
            }
        }

        private void btn_EncryptFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Select file";
            dialog.InitialDirectory = ".\\";
            dialog.Filter = "exe files (*.exe)|*.exe";
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            // First we are going to open the file streams 
            string fileOut;
            if (dialog.FileName.EndsWith(".exe"))
                fileOut = dialog.FileName.Substring(0, dialog.FileName.Length - 4) + ".db";
            else
                fileOut = dialog.FileName + ".db";
            FileStream fsIn = new FileStream(dialog.FileName,      FileMode.Open, FileAccess.Read);
            FileStream fsOut = new FileStream(fileOut,             FileMode.Create, FileAccess.Write);

            // Then we are going to derive a Key and an IV from the
            // Password and create an algorithm 
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(FConstants.StringCipherKey,
                new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d,
            0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});

            Rijndael alg = Rijndael.Create();
            alg.Key = pdb.GetBytes(32);
            alg.IV = pdb.GetBytes(16);

            // Now create a crypto stream through which we are going
            // to be pumping data. 
            // Our fileOut is going to be receiving the encrypted bytes. 
            CryptoStream cs = new CryptoStream(fsOut,
                alg.CreateEncryptor(), CryptoStreamMode.Write);

            // Now will will initialize a buffer and will be processing
            // the input file in chunks. 
            // This is done to avoid reading the whole file (which can
            // be huge) into memory. 
            int bufferLen = 4096;
            byte[] buffer = new byte[bufferLen];
            int bytesRead;
            long bytes = 0;
            do
            {
                // read a chunk of data from the input file 
                bytesRead = fsIn.Read(buffer, 0, bufferLen);

                // encrypt it 
                cs.Write(buffer, 0, bytesRead);

                richTextBox_EncryptionEncrypted.Text = $"Encrypting {bytes+=bytesRead} bytes";
            } while (bytesRead != 0);

            // close everything 

            // this will also close the unrelying fsOut stream
            cs.Close();
            fsIn.Close();
            richTextBox_EncryptionEncrypted.Text += "...Done.";
        }

        private void btn_DecryptFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Select file";
            dialog.InitialDirectory = ".\\";
            dialog.Filter = "BMP files (*.db)|*.db";
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            // First we are going to open the file streams 
            string fileOut;
            if (dialog.FileName.EndsWith(".db"))
                fileOut = dialog.FileName.Substring(0, dialog.FileName.Length - 4) + ".exe";
            else
                fileOut = dialog.FileName + ".exe";

            FileStream fsIn = new FileStream(dialog.FileName,
                        FileMode.Open, FileAccess.Read);
            FileStream fsOut = new FileStream(fileOut,
                        FileMode.Create, FileAccess.Write);

            // Then we are going to derive a Key and an IV from
            // the Password and create an algorithm 
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(FConstants.StringCipherKey,
                new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d,
            0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});
            Rijndael alg = Rijndael.Create();

            alg.Key = pdb.GetBytes(32);
            alg.IV = pdb.GetBytes(16);

            // Now create a crypto stream through which we are going
            // to be pumping data. 
            // Our fileOut is going to be receiving the Decrypted bytes. 
            CryptoStream cs = new CryptoStream(fsOut,
                alg.CreateDecryptor(), CryptoStreamMode.Write);

            // Now will will initialize a buffer and will be 
            // processing the input file in chunks. 
            // This is done to avoid reading the whole file (which can be
            // huge) into memory. 
            int bufferLen = 4096;
            byte[] buffer = new byte[bufferLen];
            int bytesRead;
            long bytes = 0;

            do
            {
                // read a chunk of data from the input file 
                bytesRead = fsIn.Read(buffer, 0, bufferLen);

                // Decrypt it 
                cs.Write(buffer, 0, bytesRead);
                richTextBox_EncryptionPlanText.Text = $"Decrypting {bytes += bytesRead} bytes";
            } while (bytesRead != 0);

            // close everything 
            cs.Close(); // this will also close the unrelying fsOut stream 
            fsIn.Close();
            richTextBox_EncryptionPlanText.Text += "...Done.";
        }
    }
}

