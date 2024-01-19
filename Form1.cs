using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace hadesFirm
{
    public class Form1 : Form
    {
        private bool SaveFileDialog = true;
        private Command.Firmware FW;
        public bool PauseDownload;
        private string destinationfile;
        private IContainer components;
        private ComboBox model_textbox;
        private Label model_lbl;
        private Button download_button;
        public RichTextBox log_textbox;
        private Label region_lbl;
        private ComboBox region_textbox;
        private Label pda_lbl;
        private TextBox pda_textbox;
        private Label csc_lbl;
        private TextBox csc_textbox;
        private Button update_button;
        private Label phone_lbl;
        private TextBox phone_textbox;
        private Label file_lbl;
        private TextBox file_textbox;
        private Label version_lbl;
        private TextBox version_textbox;
        private GroupBox groupBox1;
        private CheckBox binary_checkbox;
        private Label binary_lbl;
        private ProgressBar progressBar;
        private Button decrypt_button;
        private GroupBox groupBox2;
        private TextBox size_textbox;
        private Label size_lbl;
        private GroupBox groupBox3;
        private CheckBox checkbox_manual;
        private CheckBox checkbox_auto;
        private CheckBox checkbox_autodecrypt;
        private CheckBox checkbox_crc;
        private ToolTip tooltip_binary;
        public Label lbl_speed;
        private Label label1;
        private Label label2;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        public Label lbl_transferred;
        private Label imei_lbl;
        private TextBox imei_textbox;
        private ToolTip tooltip_binary_box;
        private Button searchButton;

        public Form1()
        {
            this.InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Logger.form = this;
            Web.form = this;
            Crypto.form = this;
            string[] models = Settings.ReadSetting<string[]>("Models");
            if (models?.Length > 0)
            {
                this.model_textbox.Items.Clear();
                this.model_textbox.Items.AddRange(models);
            }
            this.model_textbox.Text = Settings.ReadSetting<string>("Model");
            string[] regions = Settings.ReadSetting<string[]>("Regions");
            if (regions?.Length > 0)
            {
                this.region_textbox.Items.Clear();
                this.region_textbox.Items.AddRange(regions);
            }
            this.region_textbox.Text = Settings.ReadSetting<string>("Region");
            this.imei_textbox.Text = Settings.ReadSetting<string>("Imei");
            this.pda_textbox.Text = Settings.ReadSetting<string>("PDAVer");
            this.csc_textbox.Text = Settings.ReadSetting<string>("CSCVer");
            this.phone_textbox.Text = Settings.ReadSetting<string>("PHONEVer");
            if (Settings.ReadSetting<string>("AutoInfo").ToLower() == "true")
                this.checkbox_auto.Checked = true;
            else
                this.checkbox_manual.Checked = true;
            if (Settings.ReadSetting<string>("SaveFileDialog").ToLower() == "false")
                this.SaveFileDialog = false;
            if (Settings.ReadSetting<string>("BinaryNature").ToLower() == "true")
                this.binary_checkbox.Checked = true;
            if (Settings.ReadSetting<string>("CheckCRC").ToLower() == "false")
                this.checkbox_crc.Checked = false;
            if (Settings.ReadSetting<string>("AutoDecrypt").ToLower() == "false")
                this.checkbox_autodecrypt.Checked = false;
            this.tooltip_binary.SetToolTip((Control) this.binary_lbl, "Full firmware including PIT file");
            this.tooltip_binary_box.SetToolTip((Control) this.binary_checkbox, "Full firmware including PIT file");
            Logger.WriteLog("hadesFirm v" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion, false);
            ServicePointManager.ServerCertificateValidationCallback = (RemoteCertificateValidationCallback) ((senderX, certificate, chain, sslPolicyErrors) => true);
        }

        private void Form1_Close(object sender, EventArgs e)
        {
            try
            {
                Settings.SetSetting("Model", this.model_textbox.Text.ToUpper());
                Settings.SetSetting("Region", this.region_textbox.Text.ToUpper());
                Settings.SetSetting("Imei", this.imei_textbox.Text.ToUpper());
                Settings.SetSetting("PDAVer", this.pda_textbox.Text);
                Settings.SetSetting("CSCVer", this.csc_textbox.Text);
                Settings.SetSetting("PHONEVer", this.phone_textbox.Text);
                Settings.SetSetting("AutoInfo", this.checkbox_auto.Checked.ToString());
                Settings.SetSetting("SaveFileDialog", this.SaveFileDialog.ToString());
                Settings.SetSetting("BinaryNature", this.binary_checkbox.Checked.ToString());
                Settings.SetSetting("CheckCRC", this.checkbox_crc.Checked.ToString());
                Settings.SetSetting("AutoDecrypt", this.checkbox_autodecrypt.Checked.ToString());
            }
            catch { }
            this.PauseDownload = true;
            Thread.Sleep(100);
            Imports.FreeModule();
            Logger.SaveLog();
        }

        private void download_button_Click(object sender, EventArgs e)
        {
            if (this.download_button.Text == "Pause")
            {
                Utility.TaskBarProgressState(true);
                this.PauseDownload = true;
                Utility.ReconnectDownload = false;
                this.download_button.Text = "Download";
            }
            else
            {
                if (e != null && e.GetType() == typeof(Form1.DownloadEventArgs) && ((Form1.DownloadEventArgs) e).isReconnect && (this.download_button.Text == "Pause" || !Utility.ReconnectDownload))
                    return;
                if (this.PauseDownload)
                    Logger.WriteLog("Download thread is still running. Please wait.", false);
                else if (string.IsNullOrEmpty(this.file_textbox.Text))
                {
                    Logger.WriteLog("No file to download. Please check for update first.", false);
                }
                else
                {
                    if (e.GetType() != typeof(Form1.DownloadEventArgs) || !((Form1.DownloadEventArgs) e).isReconnect)
                    {
                        if (this.SaveFileDialog)
                        {
                            string str = Path.GetExtension(Path.GetFileNameWithoutExtension(this.FW.Filename)) + Path.GetExtension(this.FW.Filename);
                            this.saveFileDialog1.SupportMultiDottedExtensions = true;
                            this.saveFileDialog1.OverwritePrompt = false;
                            this.saveFileDialog1.FileName = this.FW.Filename.Replace(str, "");
                            this.saveFileDialog1.Filter = "Firmware|*" + str;
                            if (this.saveFileDialog1.ShowDialog() != DialogResult.OK)
                            {
                                Logger.WriteLog("Aborted.", false);
                                return;
                            }
                            if (!this.saveFileDialog1.FileName.EndsWith(str))
                                this.saveFileDialog1.FileName += str;
                            else
                                this.saveFileDialog1.FileName = this.saveFileDialog1.FileName.Replace(str + str, str);
                            Logger.WriteLog("Filename: " + this.saveFileDialog1.FileName, false);
                            this.destinationfile = this.saveFileDialog1.FileName;
                            if (System.IO.File.Exists(this.destinationfile))
                            {
                                switch (new customMessageBox("The destination file already exists.\r\nWould you like to append it (resume download)?", "Append", DialogResult.Yes, "Overwrite", DialogResult.No, "Cancel", DialogResult.Cancel, (Image) SystemIcons.Warning.ToBitmap()).ShowDialog())
                                {
                                    case DialogResult.Cancel:
                                        Logger.WriteLog("Aborted.", false);
                                        return;
                                    case DialogResult.No:
                                        System.IO.File.Delete(this.destinationfile);
                                        break;
                                }
                            }
                        }
                        else
                            this.destinationfile = this.FW.Filename;
                    }
                    Utility.TaskBarProgressState(false);
                    BackgroundWorker backgroundWorker = new BackgroundWorker();
                    backgroundWorker.DoWork += (DoWorkEventHandler) ((o, _e) =>
                    {
                        try
                        {
                            this.ControlsEnabled(false);
                            Utility.ReconnectDownload = false;
                            this.download_button.Invoke((Delegate) ((Action) (() =>
                            {
                                this.download_button.Enabled = true;
                                this.download_button.Text = "Pause";
                            })));
                            if (this.FW.Filename == this.destinationfile)
                                Logger.WriteLog("Trying to download " + this.FW.Filename, false);
                            else
                                Logger.WriteLog("Trying to download " + this.FW.Filename + " to " + this.destinationfile, false);
                            Command.Download(this.FW.Path, this.FW.Filename, this.FW.Version, this.FW.Region, this.FW.Model_Type, this.destinationfile, this.FW.Size, true);
                            if (this.PauseDownload)
                            {
                                Logger.WriteLog("Download paused", false);
                                this.PauseDownload = false;
                                if (Utility.ReconnectDownload)
                                {
                                    Logger.WriteLog("Reconnecting...", false);
                                    Utility.Reconnect(new Action<object, EventArgs>(this.download_button_Click));
                                }
                            }
                            else
                            {
                                Logger.WriteLog("Download finished", false);
                                if (this.checkbox_crc.Checked)
                                {
                                    if (this.FW.CRC == null)
                                    {
                                        Logger.WriteLog("Unable to check CRC. Value not set by Samsung", false);
                                    }
                                    else
                                    {
                                        Logger.WriteLog("\nChecking CRC32...", false);
                                        if (!Utility.CRCCheck(this.destinationfile, this.FW.CRC))
                                        {
                                            Logger.WriteLog("Error: CRC does not match. Please redownload the file.", false);
                                            System.IO.File.Delete(this.destinationfile);
                                            goto label_15;
                                        }
                                        else
                                            Logger.WriteLog("Success: CRC match!", false);
                                    }
                                }
                                this.decrypt_button.Invoke((Delegate) ((Action) (() => this.decrypt_button.Enabled = true)));
                                if (this.checkbox_autodecrypt.Checked)
                                    this.decrypt_button_Click(o, (EventArgs) null);
                            }
label_15:
                            if (!Utility.ReconnectDownload)
                                this.ControlsEnabled(true);
                            this.download_button.Invoke((Delegate) ((Action) (() => this.download_button.Text = "Download")));
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLog(ex.Message, false);
                            Logger.WriteLog(ex.ToString(), false);
                        }
                    });
                    backgroundWorker.RunWorkerAsync();
                }
            }
        }

        private void update_button_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.model_textbox.Text))
                Logger.WriteLog("Error: Please specify a model", false);
            else if (string.IsNullOrEmpty(this.region_textbox.Text))
                Logger.WriteLog("Error: Please specify a region", false);
            else if (string.IsNullOrEmpty(this.imei_textbox.Text))
                Logger.WriteLog("Error: Please specify an Imei or Serial number", false);
            else if (this.checkbox_manual.Checked && (string.IsNullOrEmpty(this.imei_textbox.Text) || string.IsNullOrEmpty(this.pda_textbox.Text) || string.IsNullOrEmpty(this.csc_textbox.Text) || string.IsNullOrEmpty(this.phone_textbox.Text)))
            {
                Logger.WriteLog("Error: Please specify PDA, CSC and Phone version and Imei/Serial or use Auto Method", false);
            }
            else
            {
                string model = this.model_textbox.Text.ToUpper();
                string region = this.region_textbox.Text.ToUpper();
                string imei = this.imei_textbox.Text.ToUpper();
                BackgroundWorker backgroundWorker = new BackgroundWorker();
                backgroundWorker.DoWork += (DoWorkEventHandler) ((o, _e) =>
                {
                    try
                    {
                        this.SetProgressBar(0, 0);
                        this.ControlsEnabled(false);
                        Utility.ReconnectDownload = false;
                        this.FW = !this.checkbox_auto.Checked ? Command.UpdateCheck(model, region, imei, this.pda_textbox.Text, this.csc_textbox.Text, this.phone_textbox.Text, this.pda_textbox.Text, this.binary_checkbox.Checked, false) : Command.UpdateCheckAuto(model, region, imei, this.binary_checkbox.Checked);
                        if (!string.IsNullOrEmpty(this.FW.Filename))
                        {
                            this.file_textbox.Invoke((Delegate) ((Action) (() => this.file_textbox.Text = this.FW.Filename)));
                            this.version_textbox.Invoke((Delegate) ((Action) (() => this.version_textbox.Text = this.FW.Version)));
                            this.size_textbox.Invoke((Delegate) ((Action) (() => this.size_textbox.Text = (long.Parse(this.FW.Size) / 1024L / 1024L).ToString() + " MB")));
                            this.model_textbox.Invoke((Action) (() =>
                            {
                                var items = model_textbox.Items.OfType<string>().ToList();
                                items.Add(model);
                                Settings.SetSetting("Models", items.Distinct().OrderBy(s => s));
                                items = region_textbox.Items.OfType<string>().ToList();
                                items.Add(region);
                                Settings.SetSetting("Regions", items.Distinct().OrderBy(s => s));
                            }));
                        }
                        else
                        {
                            this.file_textbox.Invoke((Delegate) ((Action) (() => this.file_textbox.Text = string.Empty)));
                            this.version_textbox.Invoke((Delegate) ((Action) (() => this.version_textbox.Text = string.Empty)));
                            this.size_textbox.Invoke((Delegate) ((Action) (() => this.size_textbox.Text = string.Empty)));
                        }
                        this.ControlsEnabled(true);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(ex.Message, false);
                        Logger.WriteLog(ex.ToString(), false);
                    }
                });
                backgroundWorker.RunWorkerAsync();
            }
        }

        public void SetProgressBar(int Progress, long bytesTransferred)
        {
            if (Progress > 100)
                Progress = 100;
            this.progressBar.Invoke((Delegate) ((Action) (() =>
            {
                this.progressBar.Value = Progress;
                if (bytesTransferred > 0)
                {
                    this.lbl_transferred.Text = $"{bytesTransferred / 1024.0 / 1024.0:0.00} MB";
                }
                else
                {
                    this.lbl_transferred.Text = "";
                }
                try
                {
                    TaskbarManager.Instance.SetProgressValue(Progress, 100);
                }
                catch (Exception ex)
                {
                }
            })));
        }

        private void ControlsEnabled(bool Enabled)
        {
            this.update_button.Invoke((Delegate) ((Action) (() => this.update_button.Enabled = Enabled)));
            this.download_button.Invoke((Delegate) ((Action) (() => this.download_button.Enabled = Enabled)));
            this.binary_checkbox.Invoke((Delegate) ((Action) (() => this.binary_checkbox.Enabled = Enabled)));
            this.model_textbox.Invoke((Delegate) ((Action) (() => this.model_textbox.Enabled = Enabled)));
            this.region_textbox.Invoke((Delegate) ((Action) (() => this.region_textbox.Enabled = Enabled)));
            this.checkbox_auto.Invoke((Delegate) ((Action) (() => this.checkbox_auto.Enabled = Enabled)));
            this.checkbox_manual.Invoke((Delegate) ((Action) (() => this.checkbox_manual.Enabled = Enabled)));
            this.checkbox_manual.Invoke((Delegate) ((Action) (() =>
            {
                if (!this.checkbox_manual.Checked)
                    return;
                this.pda_textbox.Enabled = Enabled;
                this.csc_textbox.Enabled = Enabled;
                this.phone_textbox.Enabled = Enabled;
            })));
            this.checkbox_autodecrypt.Invoke((Delegate) ((Action) (() => this.checkbox_autodecrypt.Enabled = Enabled)));
            this.checkbox_crc.Invoke((Delegate) ((Action) (() => this.checkbox_crc.Enabled = Enabled)));
        }

        private void decrypt_button_Click(object sender, EventArgs e)
        {
            if (!System.IO.File.Exists(this.destinationfile))
            {
                Logger.WriteLog("Error: File " + this.destinationfile + " does not exist", false);
            }
            else
            {
                string pda = this.pda_textbox.Text;
                string csc = this.csc_textbox.Text;
                string phone = this.phone_textbox.Text;

                BackgroundWorker backgroundWorker = new BackgroundWorker();
                backgroundWorker.DoWork += (DoWorkEventHandler) ((o, _e) =>
                {
                    Thread.Sleep(100);
                    Logger.WriteLog("\nDecrypting and unzipping firmware...", false);
                    this.ControlsEnabled(false);
                    this.decrypt_button.Invoke((Delegate) ((Action) (() => this.decrypt_button.Enabled = false)));
                    if (this.destinationfile.EndsWith(".enc2"))
                        Crypto.SetDecryptKey(this.FW.Region, this.FW.Model, this.FW.Version);
                    else if (this.destinationfile.EndsWith(".enc4"))
                    {
                        if (this.FW.BinaryNature == 1)
                            Crypto.SetDecryptKey(this.FW.Version, this.FW.LogicValueFactory);
                        else
                            Crypto.SetDecryptKey(this.FW.Version, this.FW.LogicValueHome);
                    }
                    string outputDirectory = Path.Combine(Path.GetDirectoryName(this.destinationfile), Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(this.destinationfile)));
                    if (Crypto.DecryptAndUnzip(this.destinationfile, outputDirectory, true) == 0)
                    {
                        CmdLine.SaveMeta(FW, Path.Combine(outputDirectory, "FirmwareInfo.txt"));
                        //            File.WriteAllText(Path.Combine(outputDirectory, "FirmwareInfo,txt"), $@"
                        //Model: {FW.Model}
                        //Type: {FW.Model_Type}
                        //Date: {FW.LastModified}
                        //DisplayName: {FW.DisplayName}
                        //OS: {FW.OS}
                        //Region: {FW.Region}
                        //Version: {FW.Version}
                        //PDA: {pda}
                        //CSC: {csc}
                        //Phone: {phone}
                        //");
                        System.IO.File.Delete(this.destinationfile);
                    }
                    Logger.WriteLog("Decryption finished", false);
                    this.ControlsEnabled(true);
                });
                backgroundWorker.RunWorkerAsync();
            }
        }

        private void checkbox_manual_CheckedChanged(object sender, EventArgs e)
        {
            if (!this.checkbox_auto.Checked && !this.checkbox_manual.Checked)
            {
                this.checkbox_manual.Checked = true;
            }
            else
            {
                this.checkbox_auto.Checked = !this.checkbox_manual.Checked;
                this.pda_textbox.Enabled = this.checkbox_manual.Checked;
                this.csc_textbox.Enabled = this.checkbox_manual.Checked;
                this.phone_textbox.Enabled = this.checkbox_manual.Checked;
            }
        }

        private void checkbox_auto_CheckedChanged(object sender, EventArgs e)
        {
            if (!this.checkbox_manual.Checked && !this.checkbox_auto.Checked)
            {
                this.checkbox_auto.Checked = true;
            }
            else
            {
                this.checkbox_manual.Checked = !this.checkbox_auto.Checked;
                this.pda_textbox.Enabled = !this.checkbox_auto.Checked;
                this.csc_textbox.Enabled = !this.checkbox_auto.Checked;
                this.phone_textbox.Enabled = !this.checkbox_auto.Checked;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.components != null)
                this.components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new Container();
            ComponentResourceManager resources = new ComponentResourceManager(typeof(Form1));
            model_textbox = new ComboBox();
            model_lbl = new Label();
            download_button = new Button();
            log_textbox = new RichTextBox();
            region_lbl = new Label();
            region_textbox = new ComboBox();
            pda_lbl = new Label();
            pda_textbox = new TextBox();
            csc_lbl = new Label();
            csc_textbox = new TextBox();
            update_button = new Button();
            phone_lbl = new Label();
            phone_textbox = new TextBox();
            file_lbl = new Label();
            file_textbox = new TextBox();
            version_lbl = new Label();
            version_textbox = new TextBox();
            groupBox1 = new GroupBox();
            imei_lbl = new Label();
            imei_textbox = new TextBox();
            searchButton = new Button();
            groupBox3 = new GroupBox();
            checkbox_manual = new CheckBox();
            checkbox_auto = new CheckBox();
            binary_checkbox = new CheckBox();
            binary_lbl = new Label();
            progressBar = new ProgressBar();
            decrypt_button = new Button();
            groupBox2 = new GroupBox();
            lbl_transferred = new Label();
            label1 = new Label();
            label2 = new Label();
            lbl_speed = new Label();
            checkbox_autodecrypt = new CheckBox();
            checkbox_crc = new CheckBox();
            size_textbox = new TextBox();
            size_lbl = new Label();
            tooltip_binary = new ToolTip(components);
            saveFileDialog1 = new SaveFileDialog();
            tooltip_binary_box = new ToolTip(components);
            groupBox1.SuspendLayout();
            groupBox3.SuspendLayout();
            groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // model_textbox
            // 
            model_textbox.Items.AddRange(new object[] { "SM-F936B", "SM-G970F", "SM-G973F", "SM-G975F", "SM-N970F", "SM-N975F", "SM-G998B", "SM-G996B", "SM-G991B", "SM-T865", "SM-T875", "SM-T976B" });
            model_textbox.Location = new Point(99, 25);
            model_textbox.Margin = new Padding(4, 3, 4, 3);
            model_textbox.Name = "model_textbox";
            model_textbox.Size = new Size(173, 23);
            model_textbox.TabIndex = 0;
            // 
            // model_lbl
            // 
            model_lbl.AutoSize = true;
            model_lbl.Location = new Point(9, 29);
            model_lbl.Margin = new Padding(4, 0, 4, 0);
            model_lbl.Name = "model_lbl";
            model_lbl.Size = new Size(41, 15);
            model_lbl.TabIndex = 1;
            model_lbl.Text = "Model";
            // 
            // download_button
            // 
            download_button.Location = new Point(86, 135);
            download_button.Margin = new Padding(0);
            download_button.Name = "download_button";
            download_button.Size = new Size(110, 27);
            download_button.TabIndex = 13;
            download_button.Text = "Download";
            download_button.UseVisualStyleBackColor = true;
            download_button.Click += this.download_button_Click;
            // 
            // log_textbox
            // 
            log_textbox.Location = new Point(14, 347);
            log_textbox.Margin = new Padding(4, 3, 4, 3);
            log_textbox.Name = "log_textbox";
            log_textbox.ReadOnly = true;
            log_textbox.Size = new Size(745, 159);
            log_textbox.TabIndex = 3;
            log_textbox.TabStop = false;
            log_textbox.Text = "";
            log_textbox.LinkClicked += this.LogTextBox_LinkClicked;
            // 
            // region_lbl
            // 
            region_lbl.AutoSize = true;
            region_lbl.Location = new Point(9, 59);
            region_lbl.Margin = new Padding(4, 0, 4, 0);
            region_lbl.Name = "region_lbl";
            region_lbl.Size = new Size(44, 15);
            region_lbl.TabIndex = 5;
            region_lbl.Text = "Region";
            // 
            // region_textbox
            // 
            region_textbox.Items.AddRange(new object[] { "DBT", "AUT", "BTU", "NEE", "SEK", "PHE", "ROM", "XSG", "KSA", "XXV", "EUX" });
            region_textbox.Location = new Point(99, 55);
            region_textbox.Margin = new Padding(4, 3, 4, 3);
            region_textbox.Name = "region_textbox";
            region_textbox.Size = new Size(173, 23);
            region_textbox.TabIndex = 1;
            // 
            // pda_lbl
            // 
            pda_lbl.AutoSize = true;
            pda_lbl.Location = new Point(12, 17);
            pda_lbl.Margin = new Padding(4, 0, 4, 0);
            pda_lbl.Name = "pda_lbl";
            pda_lbl.Size = new Size(30, 15);
            pda_lbl.TabIndex = 7;
            pda_lbl.Text = "PDA";
            // 
            // pda_textbox
            // 
            pda_textbox.CharacterCasing = CharacterCasing.Upper;
            pda_textbox.Location = new Point(92, 14);
            pda_textbox.Margin = new Padding(4, 3, 4, 3);
            pda_textbox.Name = "pda_textbox";
            pda_textbox.Size = new Size(173, 23);
            pda_textbox.TabIndex = 4;
            // 
            // csc_lbl
            // 
            csc_lbl.AutoSize = true;
            csc_lbl.Location = new Point(12, 47);
            csc_lbl.Margin = new Padding(4, 0, 4, 0);
            csc_lbl.Name = "csc_lbl";
            csc_lbl.Size = new Size(29, 15);
            csc_lbl.TabIndex = 9;
            csc_lbl.Text = "CSC";
            // 
            // csc_textbox
            // 
            csc_textbox.CharacterCasing = CharacterCasing.Upper;
            csc_textbox.Location = new Point(92, 44);
            csc_textbox.Margin = new Padding(4, 3, 4, 3);
            csc_textbox.Name = "csc_textbox";
            csc_textbox.Size = new Size(173, 23);
            csc_textbox.TabIndex = 5;
            // 
            // update_button
            // 
            update_button.Location = new Point(163, 277);
            update_button.Margin = new Padding(4, 3, 4, 3);
            update_button.Name = "update_button";
            update_button.Size = new Size(108, 27);
            update_button.TabIndex = 10;
            update_button.Text = "Check Update";
            update_button.UseVisualStyleBackColor = true;
            update_button.Click += this.update_button_Click;
            // 
            // phone_lbl
            // 
            phone_lbl.AutoSize = true;
            phone_lbl.Location = new Point(12, 77);
            phone_lbl.Margin = new Padding(4, 0, 4, 0);
            phone_lbl.Name = "phone_lbl";
            phone_lbl.Size = new Size(41, 15);
            phone_lbl.TabIndex = 12;
            phone_lbl.Text = "Phone";
            // 
            // phone_textbox
            // 
            phone_textbox.CharacterCasing = CharacterCasing.Upper;
            phone_textbox.Location = new Point(92, 74);
            phone_textbox.Margin = new Padding(4, 3, 4, 3);
            phone_textbox.Name = "phone_textbox";
            phone_textbox.Size = new Size(173, 23);
            phone_textbox.TabIndex = 6;
            // 
            // file_lbl
            // 
            file_lbl.AutoSize = true;
            file_lbl.Location = new Point(7, 29);
            file_lbl.Margin = new Padding(4, 0, 4, 0);
            file_lbl.Name = "file_lbl";
            file_lbl.Size = new Size(25, 15);
            file_lbl.TabIndex = 13;
            file_lbl.Text = "File";
            // 
            // file_textbox
            // 
            file_textbox.Location = new Point(88, 21);
            file_textbox.Margin = new Padding(4, 3, 4, 3);
            file_textbox.Name = "file_textbox";
            file_textbox.ReadOnly = true;
            file_textbox.Size = new Size(338, 23);
            file_textbox.TabIndex = 20;
            file_textbox.TabStop = false;
            // 
            // version_lbl
            // 
            version_lbl.AutoSize = true;
            version_lbl.Location = new Point(7, 59);
            version_lbl.Margin = new Padding(4, 0, 4, 0);
            version_lbl.Name = "version_lbl";
            version_lbl.Size = new Size(45, 15);
            version_lbl.TabIndex = 15;
            version_lbl.Text = "Version";
            // 
            // version_textbox
            // 
            version_textbox.Location = new Point(88, 51);
            version_textbox.Margin = new Padding(4, 3, 4, 3);
            version_textbox.Name = "version_textbox";
            version_textbox.ReadOnly = true;
            version_textbox.Size = new Size(338, 23);
            version_textbox.TabIndex = 30;
            version_textbox.TabStop = false;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(imei_lbl);
            groupBox1.Controls.Add(imei_textbox);
            groupBox1.Controls.Add(searchButton);
            groupBox1.Controls.Add(groupBox3);
            groupBox1.Controls.Add(checkbox_manual);
            groupBox1.Controls.Add(checkbox_auto);
            groupBox1.Controls.Add(binary_checkbox);
            groupBox1.Controls.Add(binary_lbl);
            groupBox1.Controls.Add(model_textbox);
            groupBox1.Controls.Add(model_lbl);
            groupBox1.Controls.Add(update_button);
            groupBox1.Controls.Add(region_textbox);
            groupBox1.Controls.Add(region_lbl);
            groupBox1.Location = new Point(14, 14);
            groupBox1.Margin = new Padding(4, 3, 4, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(4, 3, 4, 3);
            groupBox1.Size = new Size(306, 315);
            groupBox1.TabIndex = 17;
            groupBox1.TabStop = false;
            groupBox1.Text = "Firmware Info";
            // 
            // imei_lbl
            // 
            imei_lbl.AutoSize = true;
            imei_lbl.Location = new Point(9, 91);
            imei_lbl.Margin = new Padding(4, 0, 4, 0);
            imei_lbl.Name = "imei_lbl";
            imei_lbl.Size = new Size(63, 15);
            imei_lbl.TabIndex = 19;
            imei_lbl.Text = "Imei/Serial";
            imei_lbl.Click += this.imei_lbl_Click;
            // 
            // imei_textbox
            // 
            imei_textbox.Location = new Point(99, 83);
            imei_textbox.Margin = new Padding(4, 3, 4, 3);
            imei_textbox.Name = "imei_textbox";
            imei_textbox.Size = new Size(173, 23);
            imei_textbox.TabIndex = 18;
            // 
            // searchButton
            // 
            searchButton.Location = new Point(99, 112);
            searchButton.Margin = new Padding(2);
            searchButton.Name = "searchButton";
            searchButton.Size = new Size(173, 27);
            searchButton.TabIndex = 20;
            searchButton.Text = "Search for IMEI online";
            searchButton.Click += this.searchButton_Click;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(phone_textbox);
            groupBox3.Controls.Add(csc_lbl);
            groupBox3.Controls.Add(csc_textbox);
            groupBox3.Controls.Add(pda_lbl);
            groupBox3.Controls.Add(pda_textbox);
            groupBox3.Controls.Add(phone_lbl);
            groupBox3.Location = new Point(7, 168);
            groupBox3.Margin = new Padding(4, 3, 4, 3);
            groupBox3.Name = "groupBox3";
            groupBox3.Padding = new Padding(4, 3, 4, 3);
            groupBox3.Size = new Size(292, 103);
            groupBox3.TabIndex = 17;
            groupBox3.TabStop = false;
            // 
            // checkbox_manual
            // 
            checkbox_manual.AutoSize = true;
            checkbox_manual.Location = new Point(163, 144);
            checkbox_manual.Margin = new Padding(4, 3, 4, 3);
            checkbox_manual.Name = "checkbox_manual";
            checkbox_manual.Size = new Size(66, 19);
            checkbox_manual.TabIndex = 3;
            checkbox_manual.Text = "Manual";
            checkbox_manual.UseVisualStyleBackColor = true;
            checkbox_manual.CheckedChanged += this.checkbox_manual_CheckedChanged;
            // 
            // checkbox_auto
            // 
            checkbox_auto.AutoSize = true;
            checkbox_auto.Location = new Point(13, 142);
            checkbox_auto.Margin = new Padding(4, 3, 4, 3);
            checkbox_auto.Name = "checkbox_auto";
            checkbox_auto.Size = new Size(52, 19);
            checkbox_auto.TabIndex = 2;
            checkbox_auto.Text = "Auto";
            checkbox_auto.UseVisualStyleBackColor = true;
            checkbox_auto.CheckedChanged += this.checkbox_auto_CheckedChanged;
            // 
            // binary_checkbox
            // 
            binary_checkbox.AutoSize = true;
            binary_checkbox.Location = new Point(99, 287);
            binary_checkbox.Margin = new Padding(4, 3, 4, 3);
            binary_checkbox.Name = "binary_checkbox";
            binary_checkbox.Size = new Size(15, 14);
            binary_checkbox.TabIndex = 7;
            binary_checkbox.UseVisualStyleBackColor = true;
            // 
            // binary_lbl
            // 
            binary_lbl.AutoSize = true;
            binary_lbl.Location = new Point(9, 287);
            binary_lbl.Margin = new Padding(4, 0, 4, 0);
            binary_lbl.Name = "binary_lbl";
            binary_lbl.Size = new Size(79, 15);
            binary_lbl.TabIndex = 13;
            binary_lbl.Text = "Binary Nature";
            // 
            // progressBar
            // 
            progressBar.Location = new Point(88, 168);
            progressBar.Margin = new Padding(4, 3, 4, 3);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(338, 27);
            progressBar.TabIndex = 18;
            // 
            // decrypt_button
            // 
            decrypt_button.Enabled = false;
            decrypt_button.Location = new Point(219, 135);
            decrypt_button.Margin = new Padding(4, 3, 4, 3);
            decrypt_button.Name = "decrypt_button";
            decrypt_button.Size = new Size(148, 27);
            decrypt_button.TabIndex = 14;
            decrypt_button.Text = "Decrypt";
            decrypt_button.UseVisualStyleBackColor = true;
            decrypt_button.Click += this.decrypt_button_Click;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(lbl_transferred);
            groupBox2.Controls.Add(label1);
            groupBox2.Controls.Add(label2);
            groupBox2.Controls.Add(lbl_speed);
            groupBox2.Controls.Add(checkbox_autodecrypt);
            groupBox2.Controls.Add(checkbox_crc);
            groupBox2.Controls.Add(size_textbox);
            groupBox2.Controls.Add(size_lbl);
            groupBox2.Controls.Add(progressBar);
            groupBox2.Controls.Add(decrypt_button);
            groupBox2.Controls.Add(download_button);
            groupBox2.Controls.Add(file_lbl);
            groupBox2.Controls.Add(file_textbox);
            groupBox2.Controls.Add(version_textbox);
            groupBox2.Controls.Add(version_lbl);
            groupBox2.Location = new Point(327, 14);
            groupBox2.Margin = new Padding(4, 3, 4, 3);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(4, 3, 4, 3);
            groupBox2.Size = new Size(433, 315);
            groupBox2.TabIndex = 20;
            groupBox2.TabStop = false;
            groupBox2.Text = "Download";
            // 
            // lbl_transferred
            // 
            lbl_transferred.AutoSize = true;
            lbl_transferred.Location = new Point(145, 245);
            lbl_transferred.Margin = new Padding(4, 0, 4, 0);
            lbl_transferred.Name = "lbl_transferred";
            lbl_transferred.Size = new Size(34, 15);
            lbl_transferred.TabIndex = 41;
            lbl_transferred.Text = "0 MB";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(9, 215);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(98, 15);
            label1.TabIndex = 25;
            label1.Text = "Download speed:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(9, 245);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(99, 15);
            label2.TabIndex = 25;
            label2.Text = "Downloaded size:";
            // 
            // lbl_speed
            // 
            lbl_speed.AutoSize = true;
            lbl_speed.Location = new Point(145, 215);
            lbl_speed.Margin = new Padding(4, 0, 4, 0);
            lbl_speed.Name = "lbl_speed";
            lbl_speed.Size = new Size(40, 15);
            lbl_speed.TabIndex = 24;
            lbl_speed.Text = "0 KB/s";
            // 
            // checkbox_autodecrypt
            // 
            checkbox_autodecrypt.AutoSize = true;
            checkbox_autodecrypt.Checked = true;
            checkbox_autodecrypt.CheckState = CheckState.Checked;
            checkbox_autodecrypt.Location = new Point(220, 111);
            checkbox_autodecrypt.Margin = new Padding(4, 3, 4, 3);
            checkbox_autodecrypt.Name = "checkbox_autodecrypt";
            checkbox_autodecrypt.Size = new Size(142, 19);
            checkbox_autodecrypt.TabIndex = 12;
            checkbox_autodecrypt.Text = "Decrypt automatically";
            checkbox_autodecrypt.UseVisualStyleBackColor = true;
            // 
            // checkbox_crc
            // 
            checkbox_crc.AutoSize = true;
            checkbox_crc.Checked = true;
            checkbox_crc.CheckState = CheckState.Checked;
            checkbox_crc.Location = new Point(88, 111);
            checkbox_crc.Margin = new Padding(4, 3, 4, 3);
            checkbox_crc.Name = "checkbox_crc";
            checkbox_crc.Size = new Size(97, 19);
            checkbox_crc.TabIndex = 11;
            checkbox_crc.Text = "Check CRC32";
            checkbox_crc.UseVisualStyleBackColor = true;
            // 
            // size_textbox
            // 
            size_textbox.Location = new Point(88, 81);
            size_textbox.Margin = new Padding(4, 3, 4, 3);
            size_textbox.Name = "size_textbox";
            size_textbox.ReadOnly = true;
            size_textbox.Size = new Size(338, 23);
            size_textbox.TabIndex = 40;
            size_textbox.TabStop = false;
            // 
            // size_lbl
            // 
            size_lbl.AutoSize = true;
            size_lbl.Location = new Point(7, 87);
            size_lbl.Margin = new Padding(4, 0, 4, 0);
            size_lbl.Name = "size_lbl";
            size_lbl.Size = new Size(27, 15);
            size_lbl.TabIndex = 20;
            size_lbl.Text = "Size";
            // 
            // saveFileDialog1
            // 
            saveFileDialog1.SupportMultiDottedExtensions = true;
            // 
            // Form1
            // 
            AcceptButton = update_button;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(779, 509);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(log_textbox);
            Icon = (Icon) resources.GetObject("$this.Icon");
            Margin = new Padding(4, 3, 4, 3);
            Name = "Form1";
            Text = "hadesFirm (2k24 Edition)";
            FormClosing += this.Form1_Close;
            Load += this.Form1_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            this.ResumeLayout(false);
        }

        public class DownloadEventArgs : EventArgs
        {
            public bool isReconnect;
        }

        private void imei_lbl_Click(object sender, EventArgs e)
        {

        }
        private void OpenLinkInBrowser(string link)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = link,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening link:\n{ex.Message}");
            }
        }

        private void LogTextBox_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            OpenLinkInBrowser(e.LinkText);
        }

        private void searchButton_Click(object sender, EventArgs e)
        {
            string model = this.model_textbox.Text;

            if (string.IsNullOrEmpty(model))
            {
                MessageBox.Show("Please enter a model to search IMEI for.");
            }
            else
            {
                string searchLink = $"https://www.google.com/search?q={model}+imei+swappa";
                Logger.WriteLog($"Opening {searchLink} in browser", false);
                OpenLinkInBrowser(searchLink);
            }
        }


    }
}