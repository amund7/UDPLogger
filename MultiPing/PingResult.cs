using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml.Serialization;
using OxyPlot;
using OxyPlot.Wpf;
using System.Windows;

namespace MultiPing {

  [Serializable]
  public class PingResult : INotifyPropertyChanged {

    MainWindow _mainWindow;

    public PingResult() {
      _mainWindow = (MainWindow)Application.Current.MainWindow;
      _active = true;
      Points = new List<DataPoint>();
      Line = new LineSeries() { StrokeThickness = 1, LineStyle = LineStyle.Solid };
      PropertyChanged += (obj, args) => {
        Console.WriteLine("Property " + args.PropertyName + " changed");
        if (args.PropertyName == "active")
          _mainWindow.EnableDisableSeries(this, _active);
      };
    }

    ~PingResult() {
      Points.Clear();
    }

  // Stuff to notify UI when a value has changed
  public event PropertyChangedEventHandler PropertyChanged;
    private void NotifyPropertyChanged(String propertyName = "") {
      if (PropertyChanged != null) {
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }

    // Internal storage    private int _ttl;
    string _name;
    [XmlElement]
    private double _value;
    [XmlIgnore]
    private bool _active;
    [XmlElement]
    public List<DataPoint> Points { get; set; }
    [XmlIgnore]
    public LineSeries Line;



    // Public properties, for the UI to access

    public bool active {
      get { return _active; }
      set {
        if (_active != value)
          _active = value;
        NotifyPropertyChanged("active");
      }
    }

    [XmlElement]
    public string name {
      get { return _name; }
      set {
        if (_name != value) {
          Line.Title =  "\t" + name;
          _name = value;
          if (name.Contains("Cell"))
            Line.YAxisKey = "V";
          if (name.Contains("mAh"))
            Line.YAxisKey = "mAh";
          if (name.Contains("Vtot"))
            Line.YAxisKey = "Vtot";
          if (name == "A")
            Line.YAxisKey = "Temp";
          if (name.Contains("Temperature"))
            Line.YAxisKey = "Temp";
          NotifyPropertyChanged("name");
        }
      }
    }

    [XmlIgnore]
    public double value {
      get { return _value; }
      set {
        Points.Add(new DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(DateTime.Now),value));
        /*if (Points.Count > 1) {
          double dt = Points[Points.Count - 1].X - Points[Points.Count - 2].X;
          double a = dt / (0.99 + dt);
          Points[Points.Count - 1] = new DataPoint(Points[Points.Count - 1].X, Points[Points.Count - 2].Y * (1-a) + Points[Points.Count - 1].Y * a);
        }*/
        if (_value != value) {
          _value = value;
          Line.Title = value + "\t" + _name;
          NotifyPropertyChanged("value");
        }
      }
    }

    public PingResult(string Name, double Value, MainWindow mainwindow) {
      _mainWindow = mainwindow;
      _name = Name;
      _value = Value;
      _active = true;
      Points = new List<DataPoint>();
      Line = new LineSeries() { StrokeThickness = 1, LineStyle = LineStyle.Solid };
      Line.Title = Value + "\t" + Name;
      PropertyChanged += (obj, args) => {
        Console.WriteLine("Property " + args.PropertyName + " changed");
        if (args.PropertyName == "active")
           _mainWindow.EnableDisableSeries(this, _active);
      };
    }

  }

  [XmlRoot("ResultsCollection")]
  public class ResultsCollection /*: INotifyCollectionChanged */ {

    public ObservableCollection<PingResult> _collection;
    [NonSerialized]
    private MainWindow _mainWindow;

    public ResultsCollection() {
      _collection = new ObservableCollection<PingResult>();
      _mainWindow = (MainWindow)Application.Current.MainWindow;
    }

    public PingResult Add(string name, double value) {
      bool found = false;
      foreach (PingResult p in _collection)
        if (p.name == name) {
          found = true;
          p.value = value;  // p.value*.99+value*.01;
        }
      if (!found) {
        PingResult temp = new PingResult(name, value, _mainWindow);
        _collection.Add(temp);
        return temp;
      }
      return null;
    }
  }
}

