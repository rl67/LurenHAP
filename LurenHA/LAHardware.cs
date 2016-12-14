using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using hwBarionet;
using LABaseStuff;

namespace LAHardware
{
    /// <summary>
    /// SysHardware
    /// </summary>
    public class SysHardware
    {

        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <remarks> Declare IO devices here. In the future to go in config file?></remarks>
        private Barionet Barionet1;  

        /// <summary>
        /// Constructor. /// </summary>
        public SysHardware()
        {
            logger.Info("Hardware created.");
        }

        /// <summary>
        /// Instanciate devices. In the future read the configured devices from config file?/// </summary>
        public void LoadConfig()
        {
            Barionet1 = new Barionet("Barionet1", "Tekniskrom", "172.16.80.20", "IOConfig.xml");
            logger.Info("Hardware configured.");
        }

        /// <summary>
        /// Adds the SignalDelegate input to the Device
        /// </summary>
        /// <param name="newSignalHandler"></param>
        public void AddInputSingalToDevice(string deviceName, SignalDelegate newSignalHandler)
        {
            if(Barionet1.Name == deviceName)
            {
                Barionet1.NewValues += new SignalDelegate(newSignalHandler);
            }
        }

        public void AddOutputSignalToDevice( string deviceName, IObservable<LaBoolValue> provider)
        {
            if(Barionet1.Name == deviceName)
            {
                Barionet1.Subscribe(provider);
            }
        }

        /// <summary>
        /// Update device inputs, by executing the ReadInputs method on all devices. /// </summary>
        public void ReadInputs()
        {
            Barionet1.ReadInputs();
        }

        /// <summary>
        /// Update the device outputs, by executing the WriteOutputs method on all devices./// </summary>
        public void WriteOutputs()
        {

        }

    }
}
