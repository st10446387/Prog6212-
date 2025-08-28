using System;
using System.Collections.Generic;
using System.ComponentModel; // Win32Exception
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace LU1_TaskManager
{
    public partial class Form1 : Form
    {
        private int processId;

        // Store PID + Name directly in the ListBox (no brittle string parsing)
        private sealed class ProcItem
        {
            public int Id { get; }
            public string Name { get; }
            public ProcItem(int id, string name) { Id = id; Name = name; }
            public override string ToString() => $"-> PID: {Id}\t Name: {Name}";
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadProcessList();
        }

        private void LoadProcessList()
        {
            listBox1.Items.Clear();
            foreach (var p in Process.GetProcesses().OrderBy(p => p.ProcessName))
            {
                listBox1.Items.Add(new ProcItem(p.Id, p.ProcessName));
            }
        }

        private ProcItem SelectedProcItemOrWarn()
        {
            if (listBox1.SelectedItem is ProcItem item) return item;
            MessageBox.Show("Please select a process first.");
            return null;
        }

        private void btnStartChrome_Click(object sender, EventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "chrome",
                    Arguments = "https://www.varsitycollege.co.za",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Maximized
                };

                var proc = Process.Start(psi);
                if (proc != null)
                {
                    processId = proc.Id;
                    // MessageBox.Show(processId.ToString());
                }
                else
                {
                    // Fallback: open in default browser
                    Process.Start(new ProcessStartInfo("https://www.varsitycollege.co.za")
                    {
                        UseShellExecute = true
                    });
                }
            }
            catch (Win32Exception) // Chrome not found or not on PATH
            {
                Process.Start(new ProcessStartInfo("https://www.varsitycollege.co.za")
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnKillChrome_Click(object sender, EventArgs e)
        {
            try
            {
                var chromeProcs = Process.GetProcessesByName("chrome");
                if (chromeProcs.Length == 0)
                {
                    MessageBox.Show("No Chrome processes found.");
                    return;
                }

                foreach (var p in chromeProcs)
                {
                    try { p.Kill(); }
                    catch (Exception ex) { Debug.WriteLine(ex.Message); }
                }

                MessageBox.Show("Attempted to close all Chrome processes.");
                LoadProcessList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnEndTaskMng_Click(object sender, EventArgs e)
        {
            try
            {
                Process.GetCurrentProcess().Kill();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void btnThreads_Click(object sender, EventArgs e)
        {
            var item = SelectedProcItemOrWarn();
            if (item == null) return;

            Process theProc;
            try
            {
                theProc = Process.GetProcessById(item.Id);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            try
            {
                var sb = new StringBuilder();
                foreach (ProcessThread pt in theProc.Threads)
                {
                    string startTime;
                    try { startTime = pt.StartTime.ToShortTimeString(); }
                    catch (Win32Exception) { startTime = "(n/a)"; } // some threads deny StartTime

                    sb.AppendLine($"-> Thread ID: {pt.Id}\tStart Time: {startTime}\tPriority: {pt.PriorityLevel}");
                }
                MessageBox.Show(sb.ToString());
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show("Access denied while reading threads: " + ex.Message);
            }
        }

        private void btnLoadedModules_Click(object sender, EventArgs e)
        {
            var item = SelectedProcItemOrWarn();
            if (item == null) return;

            Process theProc;
            try
            {
                theProc = Process.GetProcessById(item.Id);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            try
            {
                var sb = new StringBuilder();
                foreach (ProcessModule pm in theProc.Modules)
                {
                    sb.AppendLine($"-> Module Name: {pm.ModuleName}");
                }
                MessageBox.Show(sb.ToString());
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show("Access denied while reading modules (common when a 32-bit app inspects 64-bit system processes): " + ex.Message);
            }
        }

        private void btnDetails_Click(object sender, EventArgs e)
        {
            var defaultAD = AppDomain.CurrentDomain;

            string output = "";
            output += "Name of this domain: " + defaultAD.FriendlyName + "\n";
            output += "ID of domain in this process: " + defaultAD.Id + "\n";
            output += "Is this the default domain? " + defaultAD.IsDefaultAppDomain() + "\n";
            output += "Base directory of this domain: " + defaultAD.BaseDirectory;

            MessageBox.Show(output);
        }

        private void btnAssemblies_Click(object sender, EventArgs e)
        {
            var defaultAD = AppDomain.CurrentDomain;

            Assembly[] loadedAssemblies = defaultAD.GetAssemblies();
            var sb = new StringBuilder();
            sb.AppendLine("Assemblies loaded in " + defaultAD.FriendlyName);

            foreach (Assembly a in loadedAssemblies)
            {
                sb.AppendLine("-> Name: " + a.GetName().Name);
                sb.AppendLine("-> Version: " + a.GetName().Version);
            }
            MessageBox.Show(sb.ToString());
        }
    }
}
