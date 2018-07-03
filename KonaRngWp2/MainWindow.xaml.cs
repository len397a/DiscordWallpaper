using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Diagnostics;
using System.ComponentModel;
using WpfAnimatedGif;

namespace KonaRngWp2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private site currentSite;
        private string currentFolder = "E:/Users/Lennart/Pictures";
        public string imagePath = "E:/Users/Lennart/Pictures";
        private string srcUrl = "http://konachan.net/post/random";
        private string file = "C:/Users/Lennart/AppData/Roaming/BetterDiscord/themes/bg.theme.css";
        private string tempPath = "E:/Users/Lennart/Pictures/tmpImg/";
        private List<site> sites;
        private List<string> subFolder = new List<string>();
        private string URL = "";
        private string filterText = "";
        private string srclink = "";
        int rating = 0;

        private readonly BackgroundWorker worker = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();
            sites = new List<site>();
            sites.Add(new site { name = "konachan.net", url = "http://konachan.net/post/random" });
            sites.Add(new site { name = "konachan.com", url = "http://konachan.com/post/random" });
            //sites.Add(new site { name = "danbooru.donmai.us", url = "https://danbooru.donmai.us/posts/random" }); //not working with tags
            sites.Add(new site { name = "gelbooru.com", url = "https://gelbooru.com/index.php?page=post&s=random"});
            sites.Add(new site { name = "local", url = "127.0.0.1" });

            comboBoxSites.ItemsSource = sites;
            comboBoxSites.DisplayMemberPath = "name";
            comboBoxSites.SelectedValuePath = "name";
            currentSite = sites.Find(x => x.name.Contains(Properties.Settings.Default.site));
            comboBoxSites.SelectedIndex = sites.FindIndex(x => x.name.Contains(Properties.Settings.Default.site));
            timeSlider.Value = Properties.Settings.Default.time;
            filter.Text = Properties.Settings.Default.tags;
            filterText = Properties.Settings.Default.tags;
            rateS.IsChecked = Properties.Settings.Default.rateS;
            rateQ.IsChecked = Properties.Settings.Default.rateQ;
            rateE.IsChecked = Properties.Settings.Default.rateE;

            subFolder.Add(imagePath);
            subFolder.AddRange(Directory.GetDirectories(imagePath));
            comboBoxFolder.ItemsSource = subFolder;
            comboBoxFolder.SelectedIndex = subFolder.FindIndex(x => x.Contains(Properties.Settings.Default.subFolder));
            currentFolder = Properties.Settings.Default.subFolder;

            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.WorkerReportsProgress = true;

            var runningProcessByName = Process.GetProcessesByName("httpd");
            if (runningProcessByName.Length == 0)
            {
                Process.Start("E:/xampp/apache_start.bat");
            }

            srcUrl = sites.Find(x => x.name.Contains(Properties.Settings.Default.site)).url;
            worker.RunWorkerAsync();
            InitTimer();
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            // run all background tasks here
            int fails = 0;
            URL = "";
            string data = "";
            if (currentSite.name.Equals("local"))
            {
                Console.WriteLine(currentFolder.Replace('\\', '/'));
                string[] allfiles = System.IO.Directory.GetFiles(currentFolder.Replace('\\','/'), "*.*", System.IO.SearchOption.AllDirectories);
                Random rnd = new Random();
                URL = "http://127.0.0.1/" + allfiles[rnd.Next(0, allfiles.Length - 1)].Substring(26).Replace('\\', '/');
            }
            else
            {
                bool ok = false;
                do
                {
                    List<string> tags;
                    data = getUrlData(currentSite.url);
                    if (filterText.Equals("") || filterText.Equals(" "))
                    {
                        tags = new List<string>();
                        ok = true;
                    }
                    else
                    {
                        tags = new List<string>(filterText.Split(' '));
                        for (int i =0; i<tags.Count; i++)
                        {
                            tags[i] = tags[i] + "<";
                        }
                        ok = true;
                    }
                    switch (rating)
                    {
                        case 1:
                            tags.Add("Rating: Safe");
                            break;
                        case 10:
                            tags.Add("Rating: Questionable");
                            break;
                        case 11:
                            tags.Add("-Rating: Explicit");
                            break;
                        case 100:
                            tags.Add("Rating: Explicit");
                            break;
                        case 101:
                            tags.Add("-Rating: Questionable");
                            break;
                        case 110:
                            tags.Add("-Rating: Safe");
                            break;
                        default:
                            break;
                    }
                    foreach (string tag in tags)
                    {
                        if (tag.Length > 0)
                        {
                            if (tag[0] == '-')
                            {
                                if (data.Contains("" + tag.Substring(1).Replace("_", " ") + ""))
                                {
                                    ok = false;
                                }
                            }
                            else
                            {
                                if (!data.Contains("" + tag.Replace("_"," ") + ""))
                                {
                                    ok = false;
                                }
                            }
                        }
                        //Console.WriteLine(tag + " " + ok);
                    }
                    if (!ok)
                    {
                        fails++;
                        worker.ReportProgress(fails);
                        //Console.WriteLine("retry...");
                        System.Threading.Thread.Sleep(100);
                    }
                    else
                    {
                        Console.WriteLine("\\[T]/");
                    }
                } while (!ok);

                if (currentSite.name.Equals("konachan.net") || currentSite.name.Equals("konachan.com") || data.Contains("konachan"))
                {

                    if (!data.Equals(""))
                    {
                        string strStart = "<a class=\"original-file-changed\" href=\"";
                        string strStart2 = "<a class=\"original-file-unchanged\" href=\"";
                        string strStart3 = "<img id=\"image\"";
                        string strStart4 = "src=\"";
                        string strEnd = "\"";
                        int Start = 0, End = 0;
                        if (data.Contains(strStart) && data.Contains(strEnd))
                        {
                            Start = data.IndexOf(strStart, 0) + strStart.Length;
                            End = data.IndexOf(strEnd, Start);
                            URL = data.Substring(Start, End - Start);
                        }
                        else if (data.Contains(strStart2) && data.Contains(strEnd))
                        {
                            Start = data.IndexOf(strStart2, 0) + strStart2.Length;
                            End = data.IndexOf(strEnd, Start);
                            URL = data.Substring(Start, End - Start);
                        }
                        else if (data.Contains(strStart3) && data.Contains(strEnd))
                        {
                            Start = data.IndexOf(strStart4, data.IndexOf(strStart3, 0) + strStart3.Length) + strStart4.Length;
                            End = data.IndexOf(strEnd, Start);
                            URL = data.Substring(Start, End - Start);
                        }
                        else
                        {
                            Console.WriteLine(data.Substring(data.IndexOf("Original ", 0) + "Original ".Length - 150, 300));
                        }
                        if (currentSite.name.Equals("konachan.net") && !URL.Equals(""))
                        {
                            URL = "https:" + URL;
                        }
                        Console.WriteLine(URL);
                        WebClient webClient = new WebClient();
                        webClient.DownloadFile(URL, tempPath + URL.Substring(URL.LastIndexOf('/') + 1).Replace("%",""));
                        URL = "http://127.0.0.1/tmpImg/" + URL.Substring(URL.LastIndexOf('/') + 1).Replace("%", "");
                    }
                }
                if (currentSite.name.Equals("danbooru.donmai.us"))
                {

                    if (!data.Equals(""))
                    {
                        string strStart = "<a id=\"image - resize - link\" href=\"";
                        string strStart2 = "<a id=\"image-resize-link\" href=\"";
                        string strEnd = "\"";
                        int Start = 0, End = 0;
                        if (data.Contains(strStart) && data.Contains(strEnd))
                        {
                            Start = data.IndexOf(strStart, 0) + strStart.Length;
                            End = data.IndexOf(strEnd, Start);
                            URL = data.Substring(Start, End - Start);
                        }
                        else if (data.Contains(strStart2) && data.Contains(strEnd))
                        {
                            Start = data.IndexOf(strStart2, 0) + strStart2.Length;
                            End = data.IndexOf(strEnd, Start);
                            URL = data.Substring(Start, End - Start);
                        }
                        else
                        {
                            Console.WriteLine(data.Substring(data.IndexOf("Original ", 0) + "Original ".Length - 150, 300));
                            Console.WriteLine(data);
                        }
                        WebClient webClient = new WebClient();
                        webClient.DownloadFile(URL, tempPath + URL.Substring(URL.LastIndexOf('/') + 1));
                        URL = "http://127.0.0.1/tmpImg/" + URL.Substring(URL.LastIndexOf('/') + 1);
                    }
                }
                if (currentSite.name.Equals("gelbooru.com")  || data.Contains("gelbooru"))
                {
                    if (!data.Equals(""))
                    {
                        string strStart = "Resize image</a></li><li><a href=\"";
                        string strStart2 = "Options</h3>";
                        string strStart3 = "Edit</a></li>";
                        string strEnd = "\"";
                        int Start = 0, End = 0;
                        if (data.Contains(strStart))
                        {
                            Start = data.IndexOf(strStart, 0) + strStart.Length;
                            End = data.IndexOf(strEnd, Start);
                            URL = data.Substring(Start, End - Start);
                        }
                        else if (data.Contains(strStart2) && data.Contains(strEnd))
                        {
                            Start = data.IndexOf(strStart2, 0) + strStart2.Length + 17;
                            End = data.IndexOf(strEnd, Start);
                            URL = data.Substring(Start, End - Start);
                        }
                        else if (data.Contains(strStart3) && data.Contains(strEnd))
                        {
                            Start = data.IndexOf(strStart3, 0) + strStart3.Length + 15;
                            End = data.IndexOf(strEnd, Start);
                            URL = data.Substring(Start, End - Start);
                        }
                        else
                        {
                            Console.WriteLine(data.Substring(data.IndexOf("Original Image", 0) + "Original Image".Length - 150, 300));
                        }
                        WebClient webClient = new WebClient();
                        webClient.DownloadFile(URL, tempPath + URL.Substring(URL.LastIndexOf('/') + 1));
                        URL = "http://127.0.0.1/tmpImg/" + URL.Substring(URL.LastIndexOf('/') + 1);
                    }
                }
            }
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //update ui once worker complete his work
            labelUrl.Content = URL;
            SrcLink.Text = srclink;

            ImageBehavior.SetAnimatedSource(image, new BitmapImage(new Uri(URL)));

            toFile(file, URL);
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            labelUrl.Content = "searching... " + e.ProgressPercentage;
        }


        private Timer timerNewImg;
        public void InitTimer()
        {
            timerNewImg = new Timer();
            timerNewImg.Tick += new EventHandler(timer1_Tick);
            timerNewImg.Interval = (int)timeSlider.Value * 1000; // in miliseconds
            timerNewImg.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            currentSite = sites.Find(x => x.name.Contains((String)comboBoxSites.SelectedValue));
            if (!worker.IsBusy)
            {
                worker.RunWorkerAsync();
            }
            timerNewImg.Interval = (int)timeSlider.Value * 1000;
        }
        
        private String getUrlData(String url)
        {
            String data = "";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            request = null;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                if (response.CharacterSet == null)
                {
                    readStream = new StreamReader(receiveStream);
                }
                else
                {
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                }

                data = readStream.ReadToEnd();

                srclink = response.ResponseUri.ToString();
                response.Close();
                readStream.Close();
            }
            return data;
        }

        private void timeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Properties.Settings.Default.time = timeSlider.Value;
            Properties.Settings.Default.Save();
        }

        private void toFile(string path, string url, string posH="center", string posV="center")
        {
            var fileContent = File.ReadLines(path).ToList();

            fileContent[fileContent.Count - 1] = "background-image: url(\"" + url + "\") !important; background-position: " + posH + " " + posV + " !important; }";
            File.WriteAllLines(path, fileContent);
        }

        private void comboBoxSites_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentSite = sites.Find(x => x.name.Contains((String)comboBoxSites.SelectedValue));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.time = timeSlider.Value;
            Properties.Settings.Default.site = (String)comboBoxSites.SelectedValue;
            Properties.Settings.Default.subFolder = (String)comboBoxFolder.SelectedValue;
            Properties.Settings.Default.tags = filter.Text;
            Properties.Settings.Default.rateS = rateS.IsChecked.Value;
            Properties.Settings.Default.rateQ = rateQ.IsChecked.Value;
            Properties.Settings.Default.rateE = rateE.IsChecked.Value;
            Properties.Settings.Default.Save();

            Process[] runningProcessByName = Process.GetProcessesByName("httpd");
            if (runningProcessByName.Length > 0)
            {
                foreach (Process runningProcess in runningProcessByName)
                {
                    runningProcess.Kill();
                }
            }
            
            System.IO.DirectoryInfo di = new DirectoryInfo(tempPath);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }


            toFile(file, "https://i.imgur.com/JwbDJtx.jpg", "center", "-100px");
        }

        private void filter_TextChanged(object sender, TextChangedEventArgs e)
        {
            filterText = filter.Text;
        }

        private void rate_Event(object sender, RoutedEventArgs e)
        {
            rating = 0;
            if (rateS.IsChecked.Value)
            {
                rating += 1;
            }
            if (rateQ.IsChecked.Value)
            {
                rating += 10;
            }
            if (rateE.IsChecked.Value)
            {
                rating += 100;
            }
        }

        private void comboBoxFolder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentFolder = (String)comboBoxFolder.SelectedValue;
        }
    }
}
