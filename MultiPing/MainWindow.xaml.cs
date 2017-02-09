/* UDPLogger 
   october 2015
   Amund Børsand 
*/

using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using System.Net;
using System.Net.Sockets;
using System.Windows.Threading;
using System.Reflection;
using OxyPlot.Wpf;
using OxyPlot.Axes;
using OxyPlot;
using Microsoft.Win32;
using System.IO;
using UDPLogger;
using System.Xml.Serialization;
using System.Linq;

namespace MultiPing {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  /// 


  public partial class MainWindow : Window {

    public static MainWindow mainWin;
    public static Dispatcher disp;
    public bool continous;

    // Our main data structure

    private ResultsCollection pingResults;

    // Public property, for the UI to access
    public ResultsCollection PingResults {
      get {
        return pingResults;
      }
    }

    UdpClient Client;

    public MainWindow() {
      pingResults = new ResultsCollection();

      InitializeComponent();

      System.Threading.Thread.CurrentThread.CurrentCulture =
        System.Globalization.CultureInfo.InvariantCulture;

      //PingList.ItemsSource = pingResults._collection;

      Plot1.Axes.Add(new OxyPlot.Wpf.DateTimeAxis());

      mainWin = this;
      disp = this.Dispatcher;

      var linearAxis = new OxyPlot.Wpf.LinearAxis();
      linearAxis.Title = "V";
      linearAxis.Key = "V";
      linearAxis.PositionTier = 1;
      linearAxis.Position = AxisPosition.Left;
      Plot1.Axes.Add(linearAxis);

      /*linearAxis = new OxyPlot.Wpf.LinearAxis();
      linearAxis.Title = "mAh";
      linearAxis.Key = "mAh";
      linearAxis.PositionTier = 2;
      linearAxis.Position = AxisPosition.Right;
      Plot1.Axes.Add(linearAxis);

      linearAxis = new OxyPlot.Wpf.LinearAxis();
      linearAxis.Title = "Vtot";
      linearAxis.Key = "Vtot";
      linearAxis.PositionTier = 2;
      linearAxis.Position = AxisPosition.Left;
      Plot1.Axes.Add(linearAxis);

      linearAxis = new OxyPlot.Wpf.LinearAxis();
      linearAxis.Title = "C/A";
      linearAxis.Key = "Temp";
      linearAxis.PositionTier = 3;
      linearAxis.Position = AxisPosition.Left;
      Plot1.Axes.Add(linearAxis);*/

      //c_ThresholdReached += pingResults._collection.CollectionChanged;

      // Get version. Crashes if within visual studio, so we have to catch that.
      try {
        var version = System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion;
        this.Title = "UDPLogger v." + version.ToString();
      } catch (Exception) {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        this.Title = "UDPLogger development build " + version.ToString();
      }
    }

    private void CallBack(IAsyncResult res) {
      IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 4444);
      byte[] received = Client.EndReceive(res, ref RemoteIpEndPoint);
      string s = Encoding.ASCII.GetString(received);
      
      Console.WriteLine(s);

      string[] lines = s.Split('\n');
      foreach (var line in lines) {
        string[] split = line.Split(':');
        if (split.Length > 1)
          MainWindow.disp.BeginInvoke(DispatcherPriority.Normal,
           new Action(() => {
             double d = 0;
             d = Double.Parse(split[1].Replace('.', ','));
             PingResult temp = pingResults.Add(split[0], d);
             if (temp != null) {
               Plot1.Series.Add(temp.Line);
               temp.Line.ItemsSource = temp.Points;
             }
             Plot1.InvalidatePlot(true);
           }));
      }

      if (continous)
        Client.BeginReceive(new AsyncCallback(CallBack), null);
    }

    public void EnableDisableSeries(PingResult p,bool enable) {
      if (enable) {
        if (!Plot1.Series.Contains(p.Line))
          Plot1.Series.Add(p.Line);
        p.Line.ItemsSource = p.Points;
      } else
        Plot1.Series.Remove(p.Line);
      Plot1.InvalidatePlot(true);
    }

    private void PingButton_Click(object sender, RoutedEventArgs e) {
      try { 
      //PingButton.IsEnabled = !continuous;
      continous = !continous;
      if (continous)
        PingButton.Content = "Stop";
      else
        PingButton.Content = "Listen";

      if (!continous) {
        return;
      }

      if (Client == null)
        Client = new UdpClient(4444);

      //Creates an IPEndPoint to record the IP Address and port number of the sender.
      // The IPEndPoint will allow you to read datagrams sent from any source.
      IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

      Client.BeginReceive(new AsyncCallback(CallBack), null);
      } catch (Exception ex) {
        MessageBox.Show(ex.ToString());
      }

    }

    private void SaveButton_Click(object sender, RoutedEventArgs e) {
      try {
        SaveFileDialog save = new SaveFileDialog();
        save.Filter = "Log|*.log";
        if ((bool)save.ShowDialog()) {
          FileStream output = new FileStream(save.FileName, FileMode.Create);
          XmlSerializer x = new XmlSerializer(pingResults.GetType());
          x.Serialize(output, pingResults);
          output.Close();
        }
      } catch (Exception ex) {
        MessageBox.Show(ex.ToString());
      }
    }

    private void CheckBox_Click_1(object sender, RoutedEventArgs e) {
      continous = (bool)((CheckBox)sender).IsChecked;
      if (!continous) PingButton.IsEnabled = true;
    }

    private async void LoadButton_Click(object sender, RoutedEventArgs e) {
      try {
        OpenFileDialog load = new OpenFileDialog();
        load.Filter = "Log|*.log|CSV|*.csv";
        if ((bool)load.ShowDialog()) {

          //Plot1.ResetAllAxes();
          Plot1.Series.Clear();
          foreach (var p in pingResults._collection) 
            p.Points.Clear();
           
          pingResults._collection.Clear();

          if (load.FilterIndex == 0) {

            XmlSerializer mySerializer = new XmlSerializer(typeof(ResultsCollection));
            FileStream myFileStream = new FileStream(load.FileName, FileMode.Open);
            // Call the Deserialize method and cast to the object type.
            pingResults = (ResultsCollection)mySerializer.Deserialize(myFileStream);
          } else {

            var f = File.OpenRead(load.FileName);
            var stream = new StreamReader(f);
            string header = stream.ReadLine();
            var columns = header.Split(',');
            int columnCount = columns.Count();
            int lineNumber = 0;
            while (!stream.EndOfStream) {
              var line = stream.ReadLine();
              int i = 0;
              await Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() => {
                foreach (var c in line.Split(',')) {
                  double d = 0;
                  if (double.TryParse(c, out d)) {
                    PingResult temp = pingResults.Add(columns[i], d, lineNumber++);
                    if (temp != null) {
                      Plot1.Series.Add(temp.Line);
                      temp.Line.ItemsSource = temp.Points;
                    }
                  }
                  i++;
                }
                //if (lineNumber % columnCount==0)
                    Plot1.InvalidatePlot(true);
              }));
            }

          }


          /*foreach (var p in pingResults._collection)
            EnableDisableSeries(p, true);*/

          Plot1.InvalidatePlot(true);

        }
      } catch (Exception ex) {
        MessageBox.Show(ex.ToString());
      }
    }

    private void Items_Click(object sender, RoutedEventArgs e) {
      ItemsWindow i = new ItemsWindow(pingResults);
    }

    void c_ThresholdReached(object sender, EventArgs e) {
      Console.WriteLine("The threshold was reached.");
    }

    private void button_Click(object sender, RoutedEventArgs e) {
      Plot1.Axes[0].InternalAxis.Reset();
      Plot1.Axes[1].InternalAxis.Reset();
      Plot1.InvalidatePlot();
    }
  }
}
