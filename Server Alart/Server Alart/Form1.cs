﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net.Mail;
using System.Net;
using System.Threading;
using System.Speech.Synthesis;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.IO;
using System.Data.OleDb;
using System.Data.SqlClient;

namespace Server_Alart
{
    public partial class Form1 : Form
    {

    public Form1()
        {
           InitializeComponent();
        }

    #region variable
    //Variable Declaration
    private static string username = Server_Alart.Properties.Settings.Default.username;
    private static string password = Server_Alart.Properties.Settings.Default.password;
    private static string serverUrl = Server_Alart.Properties.Settings.Default.serverUrl;
    private static int port = Server_Alart.Properties.Settings.Default.port;
    private string loadPath = Server_Alart.Properties.Settings.Default.load_path;
    // 32 bytes long.  Using a 16 character string here gives us 32 bytes when converted to a byte array.
    private const string initVector = "p$mg^il9_zpg*l88";
    // This constant is used to determine the keysize of the encryption algorithm
    private const int keysize = 256;
    private const string pw = "$h0BuJ";
    Thread thread1;
    Boolean _isStarted = false;
    #endregion

    private void Form1_Load(object sender, EventArgs e)
        {
            this.loadSettings();
        }

    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        try{
            if (e.CloseReason == CloseReason.UserClosing)
            {
                dynamic mbox = MessageBox.Show("Run the Application Background \"Yes\" \nClose The Application \"No\" \nCancel Closing \"Cancel\"", "Exit Confirmation", MessageBoxButtons.YesNoCancel);
                if (mbox == DialogResult.Yes)
                {
                    this.notifyIcon1.Visible = true;
                    MessageBox.Show("The System is running on Background");
                    e.Cancel = true;
                    Form1.ActiveForm.Hide();
                }
                else if (mbox == DialogResult.No)
                {
                    if (_isStarted)
                    {
                        this.thread1.Abort();
                    }
                    e.Cancel = false;
                    Application.Exit();
                }
                else if (mbox == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }
        catch (Exception ex)
        {
            this.logs(ex.ToString());
        }
    }

    #region  hostManager

    //Add new data to dataGridView
    private void button1_Click(object sender, EventArgs e)
        {
        
            addHost();
         }


        //Adds Hosts to Datagridview
    private void addHost() {
        try
        {
            string _hostName = "";
            if (this.txt_Host.Text != "")
            {
                if (this.txt_hostname.Text == "")
                {
                    try
                    {
                        _hostName = System.Net.Dns.GetHostEntry(IPAddress.Parse(txt_Host.Text)).HostName.ToString();
                    }
                    catch (Exception ex) {
                        this.logs("Host: "+txt_Host.Text+" is not reachable or have no DNS entry");
                        _hostName = "";
                    }
                }
                else {
                    _hostName = txt_hostname.Text;
                }
                int id = dataGridView1.Rows.Count;
                this.dataGridView1.Rows.Add(id,_hostName, this.txt_Host.Text, this.txt_Port.Text);
                this.logs("Server Address added to list : " + this.txt_Host.Text);
            }
        }
        catch (Exception ex)
        {
            logs(ex.ToString());
        }
    }
 

        //Checks the specified port for specific host
    private bool PingHostPort(string _HostURI, int _PortNumber)
        {
            try
            {
                TcpClient client = new TcpClient();
                IPAddress _ip = IPAddress.Parse(_HostURI);
                client.Connect(_ip, _PortNumber);
                client.Close();
                return true;
                }
            catch (Exception ex)
            {
                return false;
            }
        }

        //Check's the ICMP (Ping) port for Specified Host
    private Boolean pingHost(string hostAddr)
    {
        byte[] _buffer = new byte[64];
        Ping _ping = new Ping();
        PingReply _pingReply = _ping.Send(IPAddress.Parse(hostAddr), 5, _buffer);
        if (_pingReply.Status.ToString() == "Success")
        {
            return true;
        }
        else {
            return false;
        }
    }

        //Checks each server to specified port from DataGridView and sets the Status
    void hostStatus() {
        try
         {
            while (true)
            {
                for (int iRow = 0; iRow < dataGridView1.Rows.Count - 1; iRow++)
                {
                    string serverAddress = dataGridView1.Rows[iRow].Cells[2].Value.ToString();
                    if (dataGridView1.Rows[iRow].Cells[3].Value.ToString() == "")
                    {
                        if (pingHost(serverAddress))
                        {
                            dataGridView1.Rows[iRow].Cells[4].Value = "Up";
                        }
                        else {
                            dataGridView1.Rows[iRow].Cells[4].Value = "Down";
                            speech("Server Down!");
                        }
                    } 
                    else
                    {
                    int sport = int.Parse(dataGridView1.Rows[iRow].Cells[3].Value.ToString());
                        if (PingHostPort(serverAddress, sport) == false)
                        {
                            dataGridView1.Rows[iRow].Cells[4].Value = "Down";
                            notifyIcon1.ShowBalloonTip(800, "Server Notification", "Server " + serverAddress + "is Down", ToolTipIcon.Info);
                            speech("Server Down!");
                        }
                        else if (PingHostPort(serverAddress, sport) == true)
                        {
                            dataGridView1.Rows[iRow].Cells[4].Value = "Up";
                        }
                    }
                }
                Thread.Sleep(Server_Alart.Properties.Settings.Default.refreshRate);
            }
        }
        catch (Exception ex) {
            thread1.Abort();
            logs(ex.ToString());
        }
    }

       
    private void serverListArray(){
            StringBuilder str = new StringBuilder();
            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
            {
                if (dataGridView1.Rows[i].Cells[3].Value.ToString() == "Down")
                {
                    str.Append(dataGridView1.Rows[i].Cells[1].Value.ToString() + "\t \t | \t" + dataGridView1.Rows[i].Cells[2].Value.ToString() + "\n");
                }
            }
            logs("Server down notification sent to : " + Server_Alart.Properties.Settings.Default.username.ToString());
            sendMail(str.ToString());
    }

    private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        this.Show();
    }
        //Remove Selected Row from Datagrid
    private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
    {
        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++ )
        {
            if (dataGridView1.Rows[i].Cells[5].Selected) {
                dataGridView1.Rows.RemoveAt(i);
                logs("Server URL removed at" + dataGridView1.Rows[i].Cells[1].ToString());
            }

        }
    }

    private void txt_Port_KeyPress(object sender, KeyPressEventArgs e)
    {
        
        
    }

    

    #region features

    //Speech Function
    private void speech(string args)
    {
        SpeechSynthesizer speak = new SpeechSynthesizer();
        speak.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Adult);
        speak.Volume = 100;
        speak.Rate = 0;
        if (Server_Alart.Properties.Settings.Default.voice_settings == true)
        {
            speak.Speak(args);
        }
    }

    //E-mail Function
    private static void sendMail(string hostDetails)
    {
        var fromAddress = new MailAddress(username, "Down Server Notifier");
        var toAddress = new MailAddress(Server_Alart.Properties.Settings.Default.toMail.ToString(), "Recipent");
        string fromPassword = Decrypt(password);
        const string subject = "Server Status is detected as down";
        string body = "Sir, The following hosts are down. Please Check ASAP. \n" + hostDetails;
        MessageBox.Show(body);
        var smtp = new SmtpClient
        {
            Host = serverUrl,
            Port = port,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
        };
        using (var message = new MailMessage(fromAddress, toAddress)
        {
            Subject = subject,
            Body = body
        })
        {
            smtp.Send(message);
        }
    }

    #endregion

    #region menuestrip

    private void startMonitoringToolStripMenuItem_Click(object sender, EventArgs e)
    {
        try
        {
            if (!_isStarted)
            {
                thread1 = new Thread(hostStatus);
                thread1.Start();
                thread1.IsBackground = true;
                logs("Server Monitoring Started ");
                _isStarted = true;
            }
            else {
                thread1.Abort();
                logs("Server Monitoring Stoped ");
                _isStarted = false;
            }
        }
        catch (Exception ex) {
            logs(ex.ToString());
        }
    }

    private void importDataFromExcelToolStripMenuItem_Click(object sender, EventArgs e)
    {
        importFromExcel();
    }

    private void exportDataToExcelToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (dataGridView1.Rows.Count > 1)
        {
            ExportToExcel();
        }
        else {
            MessageBox.Show("There is no data to Export!");
        }
    }

    private void exitToolStripMenuItem_Click(object sender, EventArgs e)
    {
        //thread1.Abort();
        Application.Exit();
    }

    private void saveLogsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        // Initialize the SaveFileDialog to specify the txt extension for the file.
        saveFileDialog1.DefaultExt = "*.txt";
        saveFileDialog1.Filter = "Text Files|*.txt";

        // Determine if the user selected a file name from the saveFileDialog.
        if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK &&
           saveFileDialog1.FileName.Length > 0)
        {
            // Save the contents of the RichTextBox into the txt file.
            richTextBox1.SaveFile(saveFileDialog1.FileName, RichTextBoxStreamType.PlainText);
        }
    }

    #endregion

    #region context_tool_menu

    private void exitApplicationToolStripMenuItem_Click(object sender, EventArgs e)
    {
        thread1.Abort();
        Application.Exit();
    }

    private void showApplicationToolStripMenuItem_Click(object sender, EventArgs e)
    {
        this.Show();
        this.notifyIcon1.Visible = false;
    }

    #endregion

    #endregion

    #region setttings

    private void btn_Cancel_Click(object sender, EventArgs e)
    {
        this.loadSettings();
    }

    private void loadSettings()
    {
        this.txtUser.Text = Server_Alart.Properties.Settings.Default.username.ToString();
        this.txtPass.Text = Server_Alart.Properties.Settings.Default.password.ToString();
        this.txtServer.Text = Server_Alart.Properties.Settings.Default.serverUrl.ToString();
        this.txtPort.Text = Server_Alart.Properties.Settings.Default.port.ToString();
        this.txt_toMail.Text = Server_Alart.Properties.Settings.Default.toMail.ToString();
        this.txtValue.Text = Server_Alart.Properties.Settings.Default.refreshRate.ToString();
        if (Server_Alart.Properties.Settings.Default.run_minimized == true)
        {
            checkBox1.Checked = true;
        }
        else
        {
            checkBox1.Checked = false;
        }
        if (Server_Alart.Properties.Settings.Default.voice_settings == true)
        {
            checkBox3.Checked = true;
        }
        else
        {
            checkBox3.Checked = false;
        }
    }

    private void checkBox2_CheckedChanged_1(object sender, EventArgs e)
    {
        {
            if (this.checkBox2.Checked == true)
            {
                Server_Alart.Properties.Settings.Default.mail_sending = 1;
                logs("Email Sending Option Enabled");
            }
            else if (this.checkBox2.Checked == false)
            {
                Server_Alart.Properties.Settings.Default.mail_sending = 0;
                logs("Email Sending Option Disabled");
            }
        }
    }

    private void btn_emailUpdate_Click_1(object sender, EventArgs e)
    {
        try
        {
            if (this.txtUser.Text == "" || this.txtPass.Text == "" || this.txtServer.Text == "" || this.txtPort.Text == "" || this.txt_toMail.Text == "")
            {
                MessageBox.Show("Please insert all data! :(");
            }
            else
            {
                Server_Alart.Properties.Settings.Default.username = this.txtUser.Text;
                Server_Alart.Properties.Settings.Default.password = Encrypt(this.txtPass.Text);
                Server_Alart.Properties.Settings.Default.serverUrl = this.txtServer.Text;
                Server_Alart.Properties.Settings.Default.toMail = this.txt_toMail.Text;
                Server_Alart.Properties.Settings.Default.port = Int32.Parse(this.txtPort.Text);
                Server_Alart.Properties.Settings.Default.Save();
                logs("Email Settings Updated");
             }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString());
        }
    }

    private void btn_refreshUpdate_Click_1(object sender, EventArgs e)
    {

        try
        {
            Form1 frm_main = new Form1();
            int rate = 60000 * Int32.Parse(txtValue.Text);
            Server_Alart.Properties.Settings.Default.refreshRate = rate;
            logs("Monitoring Refresh rate have been updated");
        }
        catch (Exception ex)
        {
            logs(ex.ToString());
        }
    }

    private void checkBox3_CheckedChanged_1(object sender, EventArgs e)
    {
        if (this.checkBox3.Checked == true)
        {
            Server_Alart.Properties.Settings.Default.voice_settings = true;
            Server_Alart.Properties.Settings.Default.Save();
            logs("App voice enabled");
        }
        else if (this.checkBox3.Checked == false)
        {
            Server_Alart.Properties.Settings.Default.voice_settings = false;
            Server_Alart.Properties.Settings.Default.Save();
            logs("App voice disabled");
        }
    }

    private void checkBox1_CheckedChanged_1(object sender, EventArgs e)
    {
        if (this.checkBox1.Checked == true)
        {
            Server_Alart.Properties.Settings.Default.run_minimized = true;
            Server_Alart.Properties.Settings.Default.Save();
            logs("Popup enabled");
        }
        else if (this.checkBox1.Checked == false)
        {
            Server_Alart.Properties.Settings.Default.run_minimized = false;
            Server_Alart.Properties.Settings.Default.Save();
            logs("Popup Disabled");
        }
    }
        
        #endregion


    private void logs(string exeption) {
        DateTime time = DateTime.Now;
        this.richTextBox1.AppendText("logs# " + time.ToString() + " \t " + exeption + " \n");
    }


        //Encrypt String
    public static string Encrypt(string plainText)
    {
        string passPhrase = pw;
        byte[] initVectorBytes = Encoding.UTF8.GetBytes(initVector);
        byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
        byte[] keyBytes = password.GetBytes(keysize / 8);
        RijndaelManaged symmetricKey = new RijndaelManaged();
        symmetricKey.Mode = CipherMode.CBC;
        ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes);
        MemoryStream memoryStream = new MemoryStream();
        CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
        cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
        cryptoStream.FlushFinalBlock();
        byte[] cipherTextBytes = memoryStream.ToArray();
        memoryStream.Close();
        cryptoStream.Close();
        return Convert.ToBase64String(cipherTextBytes);
    }

        //Decrypt String
    public static string Decrypt(string cipherText)
    {
        string passPhrase = pw;
        byte[] initVectorBytes = Encoding.UTF8.GetBytes(initVector);
        byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
        PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
        byte[] keyBytes = password.GetBytes(keysize / 8);
        RijndaelManaged symmetricKey = new RijndaelManaged();
        symmetricKey.Mode = CipherMode.CBC;
        ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);
        MemoryStream memoryStream = new MemoryStream(cipherTextBytes);
        CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
        byte[] plainTextBytes = new byte[cipherTextBytes.Length];
        int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
        memoryStream.Close();
        cryptoStream.Close();
        return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
    }

    private void txt_Host_Leave(object sender, EventArgs e)
    {
        if (txt_Host.Text != "")
        {
            txt_hostname.ReadOnly = false;
        }
        else {
            txt_hostname.ReadOnly = true;
        }
    }


    /// <summary> 
    /// Exports the datagridview values to Excel. 
    /// </summary> 
    private void ExportToExcel()
    {
        try
        {
            // creating Excel Application
            Microsoft.Office.Interop.Excel._Application app = new Microsoft.Office.Interop.Excel.Application();
            // creating new WorkBook within Excel application
            Microsoft.Office.Interop.Excel._Workbook workbook = app.Workbooks.Add(Type.Missing);
            // creating new Excelsheet in workbook
            Microsoft.Office.Interop.Excel._Worksheet worksheet = null;
            // see the excel sheet behind the program
            app.Visible = true;
            worksheet = workbook.Sheets["Sheet1"];
            worksheet = workbook.ActiveSheet;
            worksheet.Name = "Sheet1";
            // Getting Headers from DatagridView
            for (int i = 1; i < dataGridView1.Columns.Count - 2; i++)
            {
                worksheet.Cells[1, i] = dataGridView1.Columns[i - 1].HeaderText;
            }
            // storing Each row and column value to excel sheet
            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
            {

                for (int j = 0; j < dataGridView1.Columns.Count - 2; j++)
                {

                    worksheet.Cells[i + 2, j + 1] = dataGridView1.Rows[i].Cells[j].Value.ToString();

                }

            }
            // save the application
            saveFileDialog1.Filter = "Excel files (*.xlsx)|*.xlsx";
            saveFileDialog1.FilterIndex = 2;
            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                workbook.SaveAs(saveFileDialog1.FileName, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlExclusive, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                MessageBox.Show("Export Successful");
            }
            app.Quit();
            logs("List Exported Sucessfully");
        }
        catch (Exception ex) {
            logs(ex.ToString());
        }
        }


    private string Excel07ConString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties='Excel 8.0;HDR={1}'";
    
    private void importFromExcel() {
        try
        {
            this.openFileDialog1.Filter = "Excel files (*.xlsx)|*.xlsx";
            this.openFileDialog1.FilterIndex = 1;
            this.openFileDialog1.Multiselect = false;
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filePath = this.openFileDialog1.FileName;
                string header = "No";
                string conStr = string.Format(Excel07ConString, filePath, header);
                using (OleDbConnection con = new OleDbConnection(conStr))
                {
                    using (OleDbCommand cmd = new OleDbCommand())
                    {
                        using (OleDbDataAdapter oda = new OleDbDataAdapter())
                        {
                            DataTable dt = new DataTable();
                            cmd.CommandText = "SELECT * From [Sheet1$]";
                            cmd.Connection = con;
                            con.Open();
                            oda.SelectCommand = cmd;
                            oda.Fill(dt);
                            con.Close();
                            if (dataGridView1.Rows.Count > 1)
                            {
                                dynamic mbox = MessageBox.Show("Do you want to clear previous IP Addresses \"Yes\" \n Keep Previous IP Addresses \"No\" ", "Confirmation", MessageBoxButtons.YesNo);
                                if (mbox == DialogResult.Yes)
                                {
                                    this.dataGridView1.Rows.Clear();
                                    this.dataGridView1.Refresh();
                                }
                            }
                            for (int i = 1; i <= dt.Rows.Count - 1; i++)
                            {
                                this.dataGridView1.Rows.Add(dt.Rows[i][0].ToString(), dt.Rows[i][1].ToString(), dt.Rows[i][2].ToString(), dt.Rows[i][3].ToString());
                            }
                        }
                    }
                }

            }
            logs("File Imported Succesfully");
        }
        catch (Exception ex) { 
        logs(ex.ToString());
        }
    }


        }
    } 