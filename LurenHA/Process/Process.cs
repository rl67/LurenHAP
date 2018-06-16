using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LABaseStuff;
using LASignals;
using NLog;
using ExtIfc;
using FileSyst;
using System.Collections.ObjectModel;


namespace LAProcess
{
    /// <summary>
    /// SysProcess
    /// </summary>
    class SysProcess
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();


             private bool diSewagePumpFaultold;      // Signal value in previous scan
        private bool diSewagePumpRunningold;

        public SignalAi temp1;
        public SignalAi temp2;
        public SignalAi temp3;
        public SignalAi temp4;
        public SignalAi temp5;
        public SignalAi temp6;
        public SignalAi temp7;
        public SignalAi temp8;

        public SignalDi diSewagePumpFault;
        public SignalDi diSewagePumpRunning;
        public SignalDi diTest;

        public SignalDo doVarmekolbe;

        // Collections
        public List<SignalDi> DiSignals;        // Collection with all DI signals
        public List<SignalAi> AiSignals;        // Collection with all AI signals
        public List<SignalDo> DoSignals;        // Collection with all DO signals
        //public ObservableCollection<SignalDo> DoSignals;


        // ************************** PROCESS SIGNAL CONFIGURATION *********************

        /// <summary>
        /// Load signals configuration. In the future to be read from the configi file?/// </summary>
        public void LoadConfig()
        {
            // Create signals.
            // Device: Barionet, Analog signals
            temp1 = new SignalAi();
            temp1.SignalSource = "Barionet1";
            temp1.Input.ChNo = 30;
            temp1.Tag = "TT-10-0001";
            temp1.Description = "GolvRetur";
            temp2 = new SignalAi();
            temp2.SignalSource = "Barionet1";
            temp2.Input.ChNo = 31;
            temp2.Tag = "TT-10-0002";
            temp2.Description = "GolvTur";
            temp3 = new SignalAi();
            temp3.SignalSource = "Barionet1";
            temp3.Input.ChNo = 32;
            temp3.Tag = "TT-10-0003";
            temp3.Description = "VarmepumpeUt";
            temp4 = new SignalAi();
            temp4.SignalSource = "Barionet1";
            temp4.Input.ChNo = 33;
            temp4.Tag = "TT-10-0004";
            temp4.Description = "VarmepumpeInn";
            temp5 = new SignalAi();
            temp5.SignalSource = "Barionet1";
            temp5.Input.ChNo = 34;
            temp5.Tag = "TT-10-0005";
            temp5.Description = "UteTemp";
            temp6 = new SignalAi();
            temp6.SignalSource = "Barionet1";
            temp6.Input.ChNo = 35;
            temp6.Tag = "TT-10-0006";
            temp6.Description = "VarmepumpeGass";
            temp7 = new SignalAi();
            temp7.SignalSource = "Barionet1";
            temp7.Input.ChNo = 36;
            temp7.Tag = "TT-10-0007";
            temp7.Description = "TankUt";
            temp8 = new SignalAi();
            temp8.SignalSource = "Barionet1";
            temp8.Input.ChNo = 37;
            temp8.Tag = "TT-10-0008";
            temp8.Description = "TankVarmevekslarUt";

            // Add signals to the AIsignalCollection
            AiSignals.Add(temp1);
            AiSignals.Add(temp2);
            AiSignals.Add(temp3);
            AiSignals.Add(temp4);
            AiSignals.Add(temp5);
            AiSignals.Add(temp6);
            AiSignals.Add(temp7);
            AiSignals.Add(temp8);

            // Device: Barionet, digital signals
            diSewagePumpFault = new SignalDi();
            diSewagePumpFault.SignalSource = "Barionet1";
            diSewagePumpFault.Input.ChNo = 1;
            diSewagePumpFault.Tag = "XA-11-0001";
            diSewagePumpFault.Description = "Kloakkpumpe systemstatus";
            diSewagePumpFault.StatusOnText = "Feil";
            diSewagePumpFault.StatusOffText = "Normal";
            diSewagePumpRunning = new SignalDi();
            diSewagePumpRunning.SignalSource = "Barionet1";
            diSewagePumpRunning.Input.ChNo = 2;
            diSewagePumpRunning.Tag = "PA-11-0001";
            diSewagePumpRunning.Description = "Kloakkpumpe driftsstatus";
            diSewagePumpRunning.StatusOnText = "Starta";
            diSewagePumpRunning.StatusOffText = "Stoppa";

            diTest = new SignalDi();
            diTest.Input.ChNo = 3;
            diTest.Tag = "XI-test";
            diTest.Description = "Force test";
            diTest.StatusOnText = "force ON";
            diTest.StatusOffText = "force OFF";
            diTest.Input.Forced = true;

            // Add DI signals to the collection
            DiSignals.Add(diSewagePumpFault);
            DiSignals.Add(diSewagePumpRunning);
            DiSignals.Add(diTest);

            doVarmekolbe = new SignalDo();
            doVarmekolbe.SignalSource = "Barionet1";
            doVarmekolbe.Output.ChNo = 1;
            doVarmekolbe.Tag = "HS-11-0001";
            doVarmekolbe.Description = "Varmekolbe";
            doVarmekolbe.StatusOnText = "På";
            doVarmekolbe.StatusOffText = "Av";

            // Add DO signals to the collection
            DoSignals.Add(doVarmekolbe);

        }

        // ************************** PROCESS LOGIC *********************

        public void Logic()
        {
            // Kloakk pumpe - feil
            if (diSewagePumpFault.Input.Value & !diSewagePumpFaultold)
            {
                SendMail.Sys10_SystemFault();
                EventLog.WriteEvent(diSewagePumpFault.EventIdentity + diSewagePumpFault.StatusOnText);
            } else if (diSewagePumpFaultold && !diSewagePumpFault.Input.Value)
            {
                EventLog.WriteEvent(diSewagePumpFault.EventIdentity + diSewagePumpFault.StatusOffText);
            }
            diSewagePumpFaultold = diSewagePumpFault.Input.Value;

            // Kloakkpumpe - start detection
            if (diSewagePumpRunning.Input.Value & !diSewagePumpRunningold)
            {
                EventLog.WriteEvent(diSewagePumpRunning.EventIdentity + diSewagePumpRunning.StatusOnText);
            }
            // Kloakkpumpe - stopp detection
            else if (diSewagePumpRunningold && !diSewagePumpRunning.Input.Value)
            {
                EventLog.WriteEvent(diSewagePumpRunning.EventIdentity + diSewagePumpRunning.StatusOffText);
            }
            diSewagePumpRunningold = diSewagePumpRunning.Input.Value;

        }

        public SysProcess()
        {
            // Create collections, objects to be added later
            DiSignals = new List<SignalDi>();
            AiSignals = new List<SignalAi>();
            DoSignals = new List<SignalDo>();
            //DoSignals = new ObservableCollection<SignalDo>();


        }
    }
}
