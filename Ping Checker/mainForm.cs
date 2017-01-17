using MaterialSkin;
using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Forms;

using Resources = Ping_Checker.Properties.Resources;

namespace League_of_Lengends_Ping_Checker
{
    public partial class mainForm : MaterialForm
    {
        // Values
        private List<Thread> threads = new List<Thread>();
        private int defaultWidth, defaultHeight, checkSleep, updateLimit, highPing, extremePing;

        // BLUE
        //private ColorScheme theme = new ColorScheme(Primary.BlueGrey800, Primary.BlueGrey900, Primary.BlueGrey500, Accent.LightBlue200, TextShade.WHITE);

        // PURPLE
        private ColorScheme theme = new ColorScheme(Primary.Indigo500, Primary.Indigo700, Primary.Indigo100, Accent.Pink200, TextShade.WHITE);

        // RED
        //private ColorScheme theme = new ColorScheme(Primary.Red500, Primary.Red700, Primary.Red50, Accent.Pink700, TextShade.WHITE);

        // GREEN
        //private ColorScheme theme = new ColorScheme(Primary.Green400, Primary.Green400, Primary.Green100, Accent.Green100, TextShade.WHITE);

        // YELLOW
        //private ColorScheme theme = new ColorScheme(Primary.Yellow800, Primary.Orange300, Primary.Yellow400, Accent.Orange100, TextShade.WHITE);

        private string[] tabsList = new string[] { "europe", "other" };
        private string[,] addressesList = new string[,] 
        {
            // EXAMPLES:
            // { " address ", " name ", " tabName " },
            // { Dns.GetHostAddresses(" url ")[0].ToString(), " name ", " tabName " },

            { "80.91.251.140", "EUNE", "europe" },
            { "188.40.93.11", "EUW", "europe" },
            { "216.52.241.254", "NA", "other" },
            { "14.200.100.27", "OCE", "other" },
            { "203.117.172.253", "GARENA", "other" },
            //{ Dns.GetHostAddresses("www.google.com")[0].ToString(), "Google.com", "other" },
            //{ Dns.GetHostAddresses("www.facebook.com")[0].ToString(), "Facebook.com", "other" },
        };

        // Main
        public mainForm()
        {
            InitializeComponent();
            InitializeTheme();
            Initialize();
        }

        // Functions
        private void InitializeTheme()
        {
            var skinManager = MaterialSkinManager.Instance;
            skinManager.AddFormToManage(this);
            skinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            skinManager.ColorScheme = theme;
        }
        private void Initialize()
        {
            // Set Priority
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;

            // Values
            checkSleep = 100; //check delay
            updateLimit = 20; //big change
            highPing = 150; //yellow alert
            extremePing = 200; //red alert

            // Resources
            toggleButton.Text = Resources.toggleButtonTextOff;

            // Functions
            InitializeControls(); //setup controls
            InitializeServers(); //setup ping checkers
            Toggle(false); //start ping checkers
        }
        private void InitializeControls()
        {
            // Resize
            if((addressesList.GetLength(0) != 0))
            {
                List<int> counter = new List<int>();
                var counterTmp = 0;

                // Find highest tab count
                for (int i = 0; i < tabsList.GetLength(0); i++)
                {
                    for (int j = 0; j < addressesList.GetLength(0); j++)
                    {
                        if(tabsList[i] == addressesList[j, 2])
                        {
                            counterTmp++;
                        }
                    }

                    counter.Add(counterTmp);
                    counterTmp = 0;
                }

                counter.Sort();
                counter.Reverse();

                // Fix size
                var increaseBy = ((counter[0] % 3) == 0) ? (counter[0] / 3) - 1 : (counter[0] / 3);

                // Lock size
                defaultWidth = Width;
                defaultHeight = Size.Height + (increaseBy * (panelTemplate.Height + panelTemplate.Margin.Top));

                Size = new Size(Size.Width, Size.Height + (increaseBy * (panelTemplate.Height + panelTemplate.Margin.Top)));
                tabControl.Size = new Size(tabControl.Width, tabControl.Height + (increaseBy * (panelTemplate.Height + panelTemplate.Margin.Top)));
            }
            else
            {
                // Lock size
                defaultWidth = Width;
                defaultHeight = Height;
            }

            // Skip first tab
            for (int i = 1; i < tabsList.GetLength(0); i++)
            {
                // Create tabs
                tabControl.TabPages.Add(tabsList[i]);
            }

            var activeTabId = 0;
            foreach (var tab in tabControl.TabPages)
            {
                activeTabId++;

                // Modify tab
                var obj = tab as TabPage;
                obj.BackColor = Color.White;
                obj.Padding = new Padding(3);

                // Change name of first tab
                if (activeTabId == 1)
                {
                    obj.Text = tabsList[0];
                }
                else
                {
                    // Create flow layout panel
                    var flowCntrl = new FlowLayoutPanel();

                    flowCntrl.Name = Resources.flowPanelControl + activeTabId.ToString();
                    flowCntrl.Dock = DockStyle.Fill;
                    flowCntrl.BackColor = Color.White;
                    obj.Controls.Add(flowCntrl);
                }

                for (int i = 0; i < addressesList.GetLength(0); i++)
                {
                    // Check location
                    if(addressesList[i, 2] == obj.Text)
                    {
                        var fixedId = i + 1;

                        // Create panel
                        var panelCntrl = new Panel();

                        panelCntrl.BackColor = Color.White;
                        panelCntrl.Size = panelTemplate.Size;
                        panelCntrl.Margin = panelTemplate.Margin;
                        panelCntrl.Visible = true;

                        // Copy controls from template
                        foreach (Control control in panelTemplate.Controls)
                        {
                            if (control is Label)
                            {
                                var cntrl = control as Label;
                                var newCntrl = new Label();

                                if (control.Text == "0") // Ping label
                                {
                                    newCntrl.Name = Resources.serverPingControl + fixedId.ToString();
                                    newCntrl.Text = Resources.calculatingText;
                                }
                                else // Name label
                                {
                                    newCntrl.Name = Resources.serverLabelControl + fixedId.ToString();
                                }

                                newCntrl.TextAlign = cntrl.TextAlign;
                                newCntrl.Location = cntrl.Location;
                                newCntrl.Size = cntrl.Size;
                                newCntrl.Font = cntrl.Font;
                                newCntrl.Visible = true;

                                panelCntrl.Controls.Add(newCntrl);
                            }
                            else if (control is MaterialProgressBar)
                            {
                                var cntrl = control as MaterialProgressBar;
                                var newCntrl = new MaterialProgressBar();

                                newCntrl.Name = Resources.serverProgressControl + fixedId.ToString();
                                newCntrl.Maximum = cntrl.Maximum;
                                newCntrl.Location = cntrl.Location;
                                newCntrl.Size = cntrl.Size;
                                newCntrl.Font = cntrl.Font;
                                newCntrl.Visible = true;

                                panelCntrl.Controls.Add(newCntrl);
                            }
                        }

                        // Apply to flow layout panel
                        var flowPanel = (FlowLayoutPanel)Controls.Find(Resources.flowPanelControl + activeTabId.ToString(), true)[0];
                        flowPanel.Controls.Add(panelCntrl);
                    }
                }
            }

            // Update labels
            for (int i = 0; i < addressesList.GetLength(0); i++)
            {
                try
                {
                    var fixedId = i + 1;
                    var label = (Label)Controls.Find(Resources.serverLabelControl + fixedId.ToString(), true)[0];
                    label.Text = addressesList[i, 1];
                }
                catch
                {
                    // Exception
                    throw new Exception("Make sure that every address is connected to a tab!");
                }
            }
        }
        private void InitializeServers()
        {
            // Clear list
            threads.Clear();

            // Create new threads
            for (int i = 0; i < addressesList.GetLength(0); i++)
            {
                threads.Add(new Thread(new ParameterizedThreadStart(ServerThread)));
            }
        }
        private void Toggle(bool forceAbort)
        {
            try
            {
                if (threads[0].IsAlive || forceAbort)
                {
                    // Abort threads
                    foreach (var thread in threads)
                    {
                        thread.Abort();
                    }

                    InitializeServers();
                }
                else
                {
                    // Start threads
                    for (int i = 0; i < threads.Count; i++)
                    {
                        threads[i].Start(i);
                    }
                }
            }
            catch
            { }
        }
        private void ServerThread(object param)
        {
            var id = (int)param;
            var fixedId = id + 1;

            var label = (Label)Controls.Find(Resources.serverPingControl + fixedId.ToString(), true)[0];
            var progress = (MaterialProgressBar)Controls.Find(Resources.serverProgressControl + fixedId.ToString(), true)[0];

            // Infinite loop
            while (true)
            {
                // Check ping
                UpdateServer(label, progress, id);
            }
        }
        private void UpdateServer(Label label, MaterialProgressBar progress, int id)
        {
            try
            {
                Ping ping = new Ping();

                PingReply pingReply = ping.Send(addressesList[id, 0], 1000);
                var timeout = (pingReply.Status != IPStatus.Success) ? true : false;

                if (!timeout)
                {
                    // Update UI
                    UpdateUI(label, progress, pingReply.RoundtripTime);
                }

                Thread.Sleep(checkSleep);
            }
            catch
            { }
        }
        private void UpdateUI(Label label, MaterialProgressBar progress, long ping)
        {
            // Use UI thread
            Invoke((MethodInvoker)delegate
            {
                // Full update
                if (progress.Value + updateLimit < (int)ping || progress.Value - updateLimit > (int)ping)
                {
                    label.Text = ping.ToString();
                    progress.Value = (ping >= progress.Maximum) ? progress.Maximum : (int)ping;
                }
                // Small increase
                else if (progress.Value < (int)ping && progress.Value != 0)
                {
                    label.Text = (int.Parse(label.Text) + 1).ToString();
                    progress.Value++;
                }
                // Small decrease
                else if (progress.Value > (int)ping)
                {
                    label.Text = (int.Parse(label.Text) - 1).ToString();
                    progress.Value--;
                }
                // Do nothing
                else if (progress.Value == (int)ping)
                { }

                if (progress.Value > extremePing)
                {
                    label.ForeColor = Color.Red;
                }
                else if (progress.Value > highPing)
                {
                    label.ForeColor = Color.Orange;
                }
                else
                {
                    label.ForeColor = SystemColors.ControlText;
                }
            });
        }

        // Events
        private void mainForm_SizeChanged(object sender, EventArgs e)
        {
            // Block size change
            Width = defaultWidth;
            Height = defaultHeight;
        }
        private void mainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Abort threads
            Toggle(true);
        }
        private void toggleButton_Click(object sender, EventArgs e)
        {
            var o = sender as MaterialRaisedButton;

            o.Text = (o.Text == Resources.toggleButtonTextOff) ? Resources.toggleButtonTextOn : Resources.toggleButtonTextOff;
            Toggle(false);
        }
    }
}