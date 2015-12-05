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
        return this.pingResults;
      }
    }

    UdpClient Client;

    public MainWindow() {
      pingResults = new ResultsCollection();

      InitializeComponent();

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

      linearAxis = new OxyPlot.Wpf.LinearAxis();
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
      Plot1.Axes.Add(linearAxis);

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
               if (split[0].Contains("Cell"))
                 temp.Line.YAxisKey = "V";
               if (split[0].Contains("mAh"))
                 temp.Line.YAxisKey = "mAh";
               if (split[0].Contains("Vtot"))
                 temp.Line.YAxisKey = "Vtot";
               if (split[0] == "A")
                 temp.Line.YAxisKey = "Temp";
               if (split[0].Contains("Temperature"))
                 temp.Line.YAxisKey = "Temp";
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
      if (enable)
        Plot1.Series.Add(p.Line);
      else
        Plot1.Series.Remove(p.Line);
      Plot1.InvalidatePlot(true);
    }

    private void PingButton_Click(object sender, RoutedEventArgs e) {
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
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e) {
      try {
        SaveFileDialog save = new SaveFileDialog();
        save.Filter = "Log|*.log";
        if ((bool)save.ShowDialog()) {
          System.IO.FileStream output = new FileStream(save.FileName, FileMode.Create);
          System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(pingResults.GetType());
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

    private void LoadButton_Click(object sender, RoutedEventArgs e) {
     // try {
        OpenFileDialog load = new OpenFileDialog();
        load.Filter = "Log|*.log";
        if ((bool)load.ShowDialog()) {
        FileStream input = new FileStream(load.FileName, FileMode.Open);
        System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(pingResults.GetType());
        var des= x.Deserialize(input);
        pingResults = (ResultsCollection)des;
        input.Close();

        /*StreamReader sr = new StreamReader("c:\\assest.xml");
        string r = sr.ReadToEnd();
        List<Asset> list;
        Type[] extraTypes = new Type[1];
        extraTypes[0] = typeof(Asset);
        System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<Asset>), extraTypes);
        object obj = serializer.Deserialize(xReader);
        list = (List<Asset>)obj;*/
      }
     // } catch (Exception ex) {
     //   MessageBox.Show(ex.ToString());
     // }
    }

    private void Items_Click(object sender, RoutedEventArgs e) {
      ItemsWindow i = new ItemsWindow(pingResults);
    }

    void c_ThresholdReached(object sender, EventArgs e) {
      Console.WriteLine("The threshold was reached.");
    }

  }
}
