using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace Gadget
{
    public partial class Hackerman : Form
    {
        //[DllImport("user32.dll")]
        //private static extern IntPtr GetForegroundWindow();

        //[DllImport("USER32.DLL")]
        //public static extern bool ShowWindow(IntPtr hWnd,int nCmdShow);

        ////  DWORD GetWindowThreadProcessId(
        ////      __in   HWND hWnd,
        ////      __out  LPDWORD lpdwProcessId
        ////  );
        //[DllImport("user32.dll")]
        //private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);



        //public static uint GetTopWindowId()
        //{
        //    IntPtr hWnd = GetForegroundWindow();
        //    uint lpdwProcessId;
        //    GetWindowThreadProcessId(hWnd, out lpdwProcessId);

        //    return lpdwProcessId;
        //}


        private Color settingColor { get; set; } = Color.White;
        public Dictionary<string, Queue<long>> watchingList { get; set; } = new Dictionary<string, Queue<long>>();

        private string key = "72482b322c07cfce3f3de7cd5049512b";

        Setting settingForm;

        private double coor_x = 48.71049, coor_y = 2.21409;
        private string city = "London";
        private double temperature, humidity;
        private string weather;

        int processID;

        public Hackerman()
        {
            InitializeComponent();

            GetColor();
            GetWatchList();
            processID = Process.GetCurrentProcess().Id;
        }

        public void ChangeColor(Color newColor)
        {
            settingColor = newColor;

            try
            {
                //Pass the filepath and filename to the StreamWriter Constructor
                StreamWriter sw = new StreamWriter("PreferredColor.txt");

                String strColor = ColorTranslator.ToHtml(settingColor);
                sw.WriteLine(strColor);

                //Close the file
                sw.Close();
            }
            catch (Exception err)
            {
                Console.WriteLine("Exception: {0}", err.Message);
            }
        }

        public void GetColor()
        {
            try
            {
                //Pass the file path and file name to the StreamReader constructor
                StreamReader sr = new StreamReader("PreferredColor.txt");

                //Read the first line of text
                String strColor = sr.ReadLine();
                settingColor = ColorTranslator.FromHtml(strColor);

                //close the file
                sr.Close();
            }
            catch (Exception err)
            {
                Console.WriteLine("Exception: {0}", err.Message);
            }
        }
        
        private void GetWatchList()
        {
            String line, key = "";
            try
            {
                //Pass the file path and file name to the StreamReader constructor
                StreamReader sr = new StreamReader("WatchList.txt");

                //Read the first line of text
                line = sr.ReadLine();

                //Continue to read until you reach end of file
                while (line != null)
                {
                    if (line.StartsWith("--"))
                    {
                        key = line.TrimStart('-');
                        watchingList.Add(key, new Queue<long>());
                    }
                    else
                    {
                        watchingList[key].Enqueue(long.Parse(line));
                    }

                    // Read a new line
                    line = sr.ReadLine();
                }

                //close the file
                sr.Close();
            }
            catch (Exception err)
            {
                Console.WriteLine("Exception: {0}", err.Message);
            }
        }

        private void SaveWatchList()
        {
            try
            {
                //Pass the filepath and filename to the StreamWriter Constructor
                StreamWriter sw = new StreamWriter("WatchList.txt");

                foreach (KeyValuePair<string, Queue<long>> entry in watchingList)
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
                Console.WriteLine("Exception: {0}", err.Message);
            }
        }

        private void UpdateWatchList()
        {
            Process[] allProcesses = Process.GetProcesses();
            List<string> checker = new List<string>();
            foreach (Process process in allProcesses)
            {
                if (watchingList.ContainsKey(process.ProcessName) && !checker.Contains(process.ProcessName))
                {
                    watchingList[process.ProcessName].Enqueue(DateTime.Now.Ticks);
                    checker.Add(process.ProcessName);
                }
            }

            panel3.Controls.Clear();
            int cnt = 0;

            foreach (KeyValuePair<string, Queue<long>> entry in watchingList)
            {
                while (entry.Value.Count > 0 && DateTime.Now.Ticks - entry.Value.Peek() > (long)7 * 24 * 60 * 60 * 10000000)
                {
                    entry.Value.Dequeue();
                }

                float value = (float)entry.Value.Count / 10;

                Label processInfo = new Label();
                processInfo.Text = value.ToString("F1") + " hr" + (value >= 2 ? "s" : "") + " on " + entry.Key;
                processInfo.ForeColor = Color.FromArgb(255,
                                        (byte)Math.Min(255, value * 10),
                                        (byte)Math.Max(0, (255 - value * 10)),
                                        0);
                processInfo.Size = new Size(panel3.Width, 15);
                processInfo.TextAlign = ContentAlignment.TopRight;

                processInfo.Location = new Point(0, 20 * cnt);
                cnt += 1;

                panel3.Controls.Add(processInfo);

                Console.WriteLine("Added {0} to panel", entry.Key);
            }
        }

        private void GetData()
        {
            try
            {
                XElement data = XElement.Parse(GetFormattedXml("http://api.openweathermap.org/data/2.5/weather?" +
                                             (city != null ? "q=" + city : "lat=" + coor_y + "&lon=" + coor_x) +
                                             "&mode=xml&units=imperial&APPID=" + key));
                Console.WriteLine(data);
                city = data.Descendants("city").First().Attribute("name").Value;
                temperature = Convert.ToDouble(data.Descendants("temperature").First().Attribute("value").Value);
                humidity = Convert.ToDouble(data.Descendants("humidity").First().Attribute("value").Value);
                weather = data.Descendants("weather").First().Attribute("value").Value;
                Console.WriteLine(weather);
            }
            catch (Exception err)
            {
                Console.WriteLine("Exception: {0}", err.Message);
            }
        }

        private void GetLocationProperty()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    // Get the response string from the URL.
                    string respond = client.DownloadString("https://api.ipdata.co/?api-key=08df24ae0727ca4c9841fdb7062e60269bd6d1385a17c479c748f1dd");

                    Console.WriteLine(respond);

                    // Load the response into an JSON object.
                    GeoData respondedObject = JsonConvert.DeserializeObject<GeoData>(respond);

                    coor_y = double.Parse(respondedObject.latitude);
                    coor_x = double.Parse(respondedObject.longitude);
                    city = respondedObject.city;

                    Console.WriteLine("Detected latitude {0} and longitude {1}", coor_y, coor_x);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("Exception: {0}", err);
                Console.WriteLine("Unknown latitude and longitude.");
            }
        }

        // Return the XML result of the URL.
        private string GetFormattedXml(string url)
        {
            // Create a web client.
            using (WebClient client = new WebClient())
            {
                // Get the response string from the URL.
                string xml = client.DownloadString(url);

                // Load the response into an XML document.
                XmlDocument xml_document = new XmlDocument();
                xml_document.LoadXml(xml);

                // Format the XML.
                using (StringWriter string_writer = new StringWriter())
                {
                    XmlTextWriter xml_text_writer =
                        new XmlTextWriter(string_writer);
                    xml_text_writer.Formatting = System.Xml.Formatting.Indented;
                    xml_document.WriteTo(xml_text_writer);

                    // Return the result.
                    return string_writer.ToString();
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (settingForm == null)
            {
                settingForm = new Setting(this);
                settingForm.Show();
            }
            else
            {
                try
                {
                    settingForm.Show();
                    settingForm.Focus();
                }
                catch (ObjectDisposedException)
                {
                    settingForm = new Setting(this);
                    settingForm.Show();
                }
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            try
            {
                // Get location and weather properties
                GetLocationProperty();
                GetData();

                // New time interval
                timer2.Interval = 360000;
            }
            catch (Exception err)
            {
                Console.WriteLine("Exception: {0}", err.Message);
            }

            try
            {
                // Update WatchList
                UpdateWatchList();
                SaveWatchList();
            }
            catch (Exception err)
            {
                Console.WriteLine("Exception: {0}", err.Message);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                WindowState = FormWindowState.Maximized;

                //if (Process.GetProcessById((int)GetTopWindowId()).ProcessName == "explorer")
                //{
                //    ShowWindow(GetForegroundWindow(), 3);
                //}
                try
                {
                    label1.Text = String.Format("{0:hh:mm:ss tt}", DateTime.Now);
                    label1.ForeColor = settingColor;
                }
                catch (Exception err)
                {
                    Console.WriteLine("Exception: {0}", err.Message);
                }

                try
                {
                    label4.Text = DateTime.Now.ToString("dddd, dd MMMM yyy");
                    label4.ForeColor = settingColor;
                }
                catch (Exception err)
                {
                    Console.WriteLine("Exception: {0}", err.Message);
                }

                try
                {
                    label2.Text = city.ToUpper();
                    label2.ForeColor = settingColor;
                }
                catch (Exception err)
                {
                    //Console.WriteLine("City: " + city);
                    Console.WriteLine("Exception: {0}", err.Message);
                }

                try
                {
                    label3.Text = (Math.Truncate((temperature - 32.0) / 1.8 * 10) / 10).ToString();
                    label3.ForeColor = settingColor;
                }
                catch (Exception err)
                {
                    Console.WriteLine("Exception: {0}", err.Message);
                }

                try
                {
                    label5.Text = weather.ToUpper();
                    label5.ForeColor = settingColor;
                }
                catch (Exception err)
                {
                    Console.WriteLine("Exception: {0}", err.Message);
                }

                try
                {
                    label6.Text = "°C";
                    label6.ForeColor = settingColor;
                }
                catch (Exception err)
                {
                    Console.WriteLine("Exception: {0}", err.Message);
                }

                try
                {
                    button1.Text = "⚙";
                    button1.ForeColor = settingColor;
                }
                catch (Exception err)
                {
                    Console.WriteLine("Exception: {0}", err.Message);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("Exception: {0}", err.Message);
            }
        }
    }


    class GeoData
    {
        public string latitude, longitude, city;
    }
}
