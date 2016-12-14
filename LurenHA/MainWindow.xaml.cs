using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
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
using System.ComponentModel;
using System.Windows.Threading;
using System.Net.Mail;
using LAHardware;
using LAProcess;
using FileSyst;


namespace LurenHA
{


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        bool sampleData;
        int tick;
        int showMode;       // Which signal type to show in data grid
        SysHardware lhaHw;
        SysProcess lhaProcess;
    

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                // Log application start
                logger.Info("Application started.");

                // System timer for ticks
                DispatcherTimer sysTimer = new DispatcherTimer();
                sysTimer.Interval = TimeSpan.FromSeconds(1);
                sysTimer.Tick += sysTimerTick;

                // UI timer
                DispatcherTimer uiTimer = new DispatcherTimer();
                uiTimer.Interval = TimeSpan.FromSeconds(2);
                uiTimer.Tick += uiTimerTick;
                
                // *************** LHA CONFIGURATION AND INITIALIZING *******************

                // Hardware -----------------
                // Create and cofigure
                lhaHw = new SysHardware();
                lhaHw.LoadConfig();

                // Process -----------------
                // Create and conigure signals
                lhaProcess = new SysProcess();
                lhaProcess.LoadConfig();

                // Link signals to the input update delegate of the hardware device input 
                lhaHw.AddInputSingalToDevice(lhaProcess.temp1.SignalSource, lhaProcess.temp1.HandleNewInpValue);
                lhaHw.AddInputSingalToDevice(lhaProcess.temp2.SignalSource, lhaProcess.temp2.HandleNewInpValue);
                lhaHw.AddInputSingalToDevice(lhaProcess.temp3.SignalSource, lhaProcess.temp3.HandleNewInpValue);
                lhaHw.AddInputSingalToDevice(lhaProcess.temp4.SignalSource, lhaProcess.temp4.HandleNewInpValue);
                lhaHw.AddInputSingalToDevice(lhaProcess.temp5.SignalSource, lhaProcess.temp5.HandleNewInpValue);
                lhaHw.AddInputSingalToDevice(lhaProcess.temp6.SignalSource, lhaProcess.temp6.HandleNewInpValue);
                lhaHw.AddInputSingalToDevice(lhaProcess.temp7.SignalSource, lhaProcess.temp7.HandleNewInpValue);
                lhaHw.AddInputSingalToDevice(lhaProcess.temp8.SignalSource, lhaProcess.temp8.HandleNewInpValue);

                lhaHw.AddInputSingalToDevice(lhaProcess.diSewagePumpFault.SignalSource, lhaProcess.diSewagePumpFault.HandleNewInpValue);
                lhaHw.AddInputSingalToDevice(lhaProcess.diSewagePumpRunning.SignalSource, lhaProcess.diSewagePumpRunning.HandleNewInpValue);
                lhaHw.AddInputSingalToDevice(lhaProcess.diTest.SignalSource, lhaProcess.diTest.HandleNewInpValue);

                // Link DO to output device
                lhaHw.AddOutputSignalToDevice(lhaProcess.doVarmekolbe.SignalSource, lhaProcess.doVarmekolbe);

                // System Timers --------------
                sysTimer.Start();
                uiTimer.Start();

            }
            catch (Exception e)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\lha\Logs\LAHome.exe.log", true))
                {
                    file.WriteLine(e.Message);
                }
            }
        }

        // System Timer for internal ticks
        void sysTimerTick(object sender, EventArgs e)
        {
            tick += 1;
            sampleData = tick % 10  == 0;

            if (sampleData)
            {
                lhaHw.ReadInputs();

                lhaProcess.Logic();
                //lhaHw.WriteOutputs();
                ////lhaLogger.LogIt();
            }
        }

        // *********************** USER INTERFACE HANDLERS ***********************

        // Timer to update the UI
        void uiTimerTick(object sender, EventArgs e)
        {
            // Show plant AI signals
            if (showMode==0)
            {
                var grndFloorAiDataSet = from aiSignal in lhaProcess.AiSignals
                                         orderby aiSignal.Tag
                                         select new { Tag = aiSignal.Tag, Desc = aiSignal.Description, Value = aiSignal.Input.Value };
                dgGroundFloor.ItemsSource = grndFloorAiDataSet;
            }
            // Show plant DI signals
            else if(showMode==1)
            {
                var grndFloorDiDataSet = from diSignal in lhaProcess.DiSignals
                                         orderby diSignal.Tag
                                         select new { Tag = diSignal.Tag, Desc = diSignal.Description, Value = diSignal.ValueStatusText };
                dgGroundFloor.ItemsSource = grndFloorDiDataSet;
            }
            // Show AI signal of device
            else if(showMode==2)
            {
                var devicesAiDataSet = from aiSignal in lhaProcess.AiSignals
                                       orderby aiSignal.Input.ChNo
                                       select new { ChNo = aiSignal.Input.ChNo, Tag = aiSignal.Tag, Desc = aiSignal.Description, Value = aiSignal.Input.Value };
                dgDevices.ItemsSource = devicesAiDataSet;
            }
            // Show DI signal of device
            else if(showMode==3)
            {
                var devicesDiDataSet = from diSignal in lhaProcess.DiSignals
                                       orderby diSignal.Input.ChNo
                                       select new
                                       {
                                           ChNo = diSignal.Input.ChNo,
                                           Tag = diSignal.Tag,
                                           Desc = diSignal.Description,
                                           Value = diSignal.Input.Value,
                                           Forced = diSignal.Input.Forced
                                       };
                dgDevices.ItemsSource = devicesDiDataSet;
            }
            // Show DO signal of device
            else if(showMode==4)
            {
                var devicesDoDataSet = from doSignal in lhaProcess.DoSignals
                                       orderby doSignal.Output.ChNo
                                       select new { ChNo = doSignal.Output.ChNo, Tag = doSignal.Tag, Desc = doSignal.Description, Value = doSignal.Output.Value };
                dgDevices.ItemsSource = devicesDoDataSet;
            }

            // Tab control - Event list
            if(tabControl.SelectedIndex==2)
            {
                string[] events = EventLog.GetEvets();

                var eventListDataSet = from ev in events
                                       let eventFields = ev.Split('\t')
                                       orderby eventFields[0] descending
                                       select new { TimeStamp = eventFields[0], Tag = eventFields[1], Desc = eventFields[2], Message = eventFields[3] };

                dgEventList.ItemsSource = eventListDataSet;
            }
        }


        private void btnShowAis_Click(object sender, RoutedEventArgs e)
        {
            showMode = 0;   // Show plant AI signals

            var grndFloorAiDataSet = from aiSignal in lhaProcess.AiSignals
                                     orderby aiSignal.Tag
                                     select new { Tag = aiSignal.Tag, Desc = aiSignal.Description, Value = aiSignal.Input.Value };

            dgGroundFloor.ItemsSource = grndFloorAiDataSet;
        }

        private void btnShowDis_Click(object sender, RoutedEventArgs e)
        {
            showMode = 1;   // Show plant DI signals

            var grndFloorDiDataSet = from diSignal in lhaProcess.DiSignals
                                     orderby diSignal.Tag
                                     select new { Tag = diSignal.Tag, Desc = diSignal.Description, Value = diSignal.ValueStatusText };
            dgGroundFloor.ItemsSource = grndFloorDiDataSet;
        }

        private void btnShowDevAis_Click(object sender, RoutedEventArgs e)
        {
            showMode = 2;   // Show Devices AI signals

            var devicesAiDataSet = from aiSignal in lhaProcess.AiSignals
                                     orderby aiSignal.Input.ChNo
                                     select new { ChNo = aiSignal.Input.ChNo, Tag = aiSignal.Tag, Desc = aiSignal.Description, Value = aiSignal.Input.Value };
            dgDevices.ItemsSource = devicesAiDataSet;
        }

        private void btnShowDevDis_Click(object sender, RoutedEventArgs e)
        {
            showMode = 3;   // Show Devices DI signals

            var devicesDiDataSet = from diSignal in lhaProcess.DiSignals
                                     orderby diSignal.Input.ChNo
                                     select new { ChNo = diSignal.Input.ChNo, Tag = diSignal.Tag, Desc = diSignal.Description, Value = diSignal.Input.Value,
                                                  Forced = diSignal.Input.Forced};
            dgDevices.ItemsSource = devicesDiDataSet;

        }

        private void btnShowDevDos_Click(object sender, RoutedEventArgs e)
        {
            showMode = 4;   // Show Devices DO signal

            //var devicesDoDataSet = from doSignal in lhaProcess.DoSignals
            //                       orderby doSignal.Output.ChNo
            //                       select new { ChNo = doSignal.Output.ChNo, Tag = doSignal.Tag, Desc = doSignal.Description, Value = doSignal.Output.Value, Forced = doSignal.Output.Forced };
            //dgDevices.ItemsSource = devicesDoDataSet;

            dgDevices.ItemsSource = lhaProcess.DoSignals;
        }
    }
}
