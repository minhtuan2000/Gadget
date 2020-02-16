using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Gadget
{
    public partial class Setting : Form
    {
        protected Hackerman CallingForm;

        public Setting(Hackerman gadget)
        {
            InitializeComponent();

            CallingForm = gadget;

            foreach (KeyValuePair<string, Queue<long>> entry in CallingForm.watchingList)
            {
                comboBox2.Items.Add(entry.Key);
            }
        }

        public void AddLog(string message)
        {
            Console.WriteLine(message);
            listBox1.Items.Add(message);
        }

        private void SaveWatchList()
        {
            try
            {
                //Pass the filepath and filename to the StreamWriter Constructor
                StreamWriter sw = new StreamWriter("WatchList.txt");

                foreach (KeyValuePair<string, Queue<long>> entry in CallingForm.watchingList)
                {
                    sw.WriteLine("--" + entry.Key);
                    foreach (long value in entry.Value)
                    {
                        sw.WriteLine(value);
                    }
                }

                //Close the file
                sw.Close();
            }
            catch (Exception err)
            {
                AddLog("Exception: " + err.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ColorDialog colorDlg = new ColorDialog();
            colorDlg.AllowFullOpen = false;
            colorDlg.AnyColor = true;
            colorDlg.SolidColorOnly = false;
            colorDlg.Color = Color.White;

            if (colorDlg.ShowDialog() == DialogResult.OK)
            {
                CallingForm.settingColor = colorDlg.Color;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Interval = 60000;
            comboBox1.Items.Clear();

            Process[] allProcesses = Process.GetProcesses();
            foreach (Process process in allProcesses)
            {
                comboBox1.Items.Add(process.ProcessName);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cmb = (ComboBox)sender;
            String selectedValue = cmb.SelectedItem.ToString();
            if (!CallingForm.watchingList.ContainsKey(selectedValue))
            {
                AddLog("Add " + selectedValue + " to watch");
                comboBox2.Items.Add(selectedValue);
                CallingForm.watchingList.Add(selectedValue, new Queue<long>());

                // Save watchlist
                SaveWatchList();
            }
            else
            {
                AddLog(selectedValue + " is alrealdy in watch list");
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cmb = (ComboBox)sender;
            String selectedValue = cmb.SelectedItem.ToString();
            if (CallingForm.watchingList.ContainsKey(selectedValue))
            {
                AddLog("Remove " + selectedValue + " from watch list");
                comboBox2.Items.Remove(selectedValue);
                CallingForm.watchingList.Remove(selectedValue);

                // Save watchlist
                SaveWatchList();
            }
            else
            {
                AddLog(selectedValue + " is not in watch list");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                Application.Restart();
            }
            catch (Exception err)
            {
                AddLog("Exception: " + err.Message);
            }
        }
    }
}
