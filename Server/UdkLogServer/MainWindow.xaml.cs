using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace UdkLogServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string PIPE_NAME = "UdkManagedLog";
        private static readonly int MAX_LOG_CAP = 1024 * 4;

        private delegate void OnLogReceivedDelegate(Log log);

        private Queue<Log> m_logs = new Queue<Log>();
        ObservableCollection<Log> m_logsOC;

        private StringCollection m_types = new StringCollection();
        private StringCollection m_channels = new StringCollection();
        private StringCollection m_sources = new StringCollection();

        private StringCollection m_selectedTypes = new StringCollection();
        private StringCollection m_selectedChannels = new StringCollection();
        private StringCollection m_selectedSources = new StringCollection();

        private StreamWriter m_logFileSW;

        public MainWindow()
        {
            string path = Environment.CurrentDirectory;
            if (!path.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
                path += System.IO.Path.DirectorySeparatorChar.ToString();
            path += "Logs";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            path += System.IO.Path.DirectorySeparatorChar.ToString();
            string logFilePath = string.Format("{0}ULog__{1}.txt", path, DateTime.Now.ToString("yyyy_MM_dd__HH_mm_ss"));

            InitializeComponent();

            try
            {
                m_logFileSW = new StreamWriter(logFilePath, true, Encoding.UTF8);
                m_logFileSW.AutoFlush = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.None);
                Environment.Exit(2);
            }

            m_logs = new Queue<Log>();
            m_logsOC = new ObservableCollection<Log>(m_logs);
            logDataGrid.ItemsSource = m_logsOC;

            ResetForm();

            Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
            Thread thread = new Thread(new ThreadStart(Listen));
            thread.IsBackground = true;
            thread.Start();
        }

        private void OnLogReceived(Log log)
        {
            string type = log.Type.Trim();
            if (type != string.Empty && !m_types.Contains(type))
            {
                m_types.Add(type);
                typesListBox.Items.Add(type);
            }

            string channel = log.Channel.Trim();
            if (channel != string.Empty && !m_channels.Contains(channel))
            {
                m_channels.Add(channel);
                channelsListBox.Items.Add(channel);
            }

            string source = log.Source.Trim();
            if (source != string.Empty && !m_sources.Contains(source))
            {
                m_sources.Add(source);
                sourcesListBox.Items.Add(source);
            }

            if ((m_selectedTypes.Count == 0 || m_selectedTypes.Contains("All") || m_selectedTypes.Contains(type))
                && (m_selectedChannels.Count == 0 || m_selectedChannels.Contains("All") || m_selectedChannels.Contains(channel))
                && (m_selectedSources.Count == 0 || m_selectedSources.Contains("All") || m_selectedSources.Contains(source)))
            {
                m_logsOC.Add(log);

                if (scrollOnOutputCheckBox.IsChecked.Value)
                {
                    // we don't need to check
                    // if (logDataGrid.Items.Count > 0 )
                    // because we already added a child
                    // and it's totally safe
                    logDataGrid.ScrollIntoView(logDataGrid.Items[logDataGrid.Items.Count - 1]);
                }
            }
        }

        private void Listen()
        {
            try
            {
                while (true)
                {
                    using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(PIPE_NAME, PipeDirection.In))
                    {
                        pipeServer.WaitForConnection();

                        try
                        {
                            using (StreamReader sr = new StreamReader(pipeServer))
                            {
                                string log = sr.ReadToEnd();
                                JavaScriptSerializer jss = new JavaScriptSerializer();
                                Log logObject = (Log)jss.Deserialize(log, typeof(Log));
                                m_logFileSW.WriteLine(string.Format("{0} {1} {2} {3} {4} {5} {6} {7}",
                                    logObject.Timestamp, logObject.Type, logObject.Channel, logObject.Source, logObject.Condition,
                                    logObject.Message, logObject.ClassName, logObject.StateName, logObject.FuncName));
                                while (m_logs.Count >= MAX_LOG_CAP)
                                {
                                    m_logs.Dequeue();
                                }
                                m_logs.Enqueue(logObject);
                                Dispatcher.Invoke(DispatcherPriority.Background,
                                    new OnLogReceivedDelegate(OnLogReceived), logObject);
                            }
                        }
                        finally
                        {
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.None);
                Environment.Exit(1);
            }
            finally
            {
            }
        }

        private void RefreshLogDataGrid()
        {
            m_logsOC.Clear();
            for (int i = 0; i < m_logs.Count; ++i)
            {
                if ((m_selectedTypes.Count == 0 || m_selectedTypes.Contains("All") || m_selectedTypes.Contains(m_logs.ToList()[i].Type.Trim()))
                    && (m_selectedChannels.Count == 0 || m_selectedChannels.Contains("All") || m_selectedChannels.Contains(m_logs.ToList()[i].Channel.Trim()))
                    && (m_selectedSources.Count == 0 || m_selectedSources.Contains("All") || m_selectedSources.Contains(m_logs.ToList()[i].Source.Trim())))
                {
                    m_logsOC.Add(m_logs.ToList()[i]);
                }
            }
        }

        private void ResetForm()
        {
            typesListBox.Items.Clear();
            channelsListBox.Items.Clear();
            sourcesListBox.Items.Clear();

            typesListBox.Items.Add("All");
            channelsListBox.Items.Add("All");
            sourcesListBox.Items.Add("All");

            m_logs.Clear();
            m_logsOC.Clear();

            m_types.Clear();
            m_channels.Clear();
            m_sources.Clear();
        }

        private void stayOnTopCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            this.Topmost = stayOnTopCheckBox.IsChecked.Value;
        }

        private void stayOnTopCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Topmost = stayOnTopCheckBox.IsChecked.Value;
        }

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        private void typesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (typesListBox.SelectedItems.Contains("All"))
            {
                m_selectedTypes.Clear();
                m_selectedTypes.Add("All");
                typesListBox.SelectedValue = typesListBox.SelectedItems[0];
                typesListBox.SelectedIndex = 0;
            }
            else
            {
                if (typesListBox.SelectedItems.Count > 0)
                {
                    m_selectedTypes.Clear();
                    for (int i = 0; i < typesListBox.SelectedItems.Count; ++i)
                    {
                        m_selectedTypes.Add(typesListBox.SelectedItems[i].ToString().Trim());
                    }
                }
                else
                {
                    m_selectedTypes.Clear();
                }
            }

            RefreshLogDataGrid();
        }

        private void channelsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (channelsListBox.SelectedItems.Contains("All"))
            {
                m_selectedChannels.Clear();
                m_selectedChannels.Add("All");
                channelsListBox.SelectedValue = channelsListBox.SelectedItems[0];
                channelsListBox.SelectedIndex = 0;
            }
            else
            {
                if (channelsListBox.SelectedItems.Count > 0)
                {
                    m_selectedChannels.Clear();
                    for (int i = 0; i < channelsListBox.SelectedItems.Count; ++i)
                    {
                        m_selectedChannels.Add(channelsListBox.SelectedItems[i].ToString().Trim());
                    }
                }
                else
                {
                    m_selectedChannels.Clear();
                }
            }

            RefreshLogDataGrid();
        }

        private void sourcesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sourcesListBox.SelectedItems.Contains("All"))
            {
                m_selectedSources.Clear();
                m_selectedSources.Add("All");
                sourcesListBox.SelectedValue = sourcesListBox.SelectedItems[0];
                sourcesListBox.SelectedIndex = 0;
            }
            else
            {
                if (sourcesListBox.SelectedItems.Count > 0)
                {
                    m_selectedSources.Clear();
                    for (int i = 0; i < sourcesListBox.SelectedItems.Count; ++i)
                    {
                        m_selectedSources.Add(sourcesListBox.SelectedItems[i].ToString().Trim());
                    }
                }
                else
                {
                    m_selectedSources.Clear();
                }
            }

            RefreshLogDataGrid();
        }
    }
}
