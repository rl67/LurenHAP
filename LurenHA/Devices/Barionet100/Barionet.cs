using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Xml;
using NLog;
using CgiScripts;
using LABaseStuff;

namespace hwBarionet
{

    /// <summary>
    /// Barionet
    /// </summary>
    class Barionet : LABase, IObserver<LaBoolValue>
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        // Property section
        private string m_ip;
        private string m_location; 
        private string m_xmlFile;
        private static string m_barionetUrl;

        // Internal variables
        private CgiSetOutput CgiIfc;
        private IDisposable unsubscriber;

        // Internal constants
        private const int cNoOfDis = 2;             // Number of digital inputs
        private const int cDiInputBase = 205;       // Barionet base address for DI inputs
        private const int cNoOfDos = 2;             // Number of digital outputs

        private const int cNoOfDallasTemps = 8;     // Number of Dallas temeparature measurements
        private const int cDallasTempFirstCh = 30;  // First Dallas temperature channel
        private const int cDallasTempLastCh = 37;   // Last Dallas temperature channel
        private const int cDallasTempsBase = 601;   // Barionet base address for Dallas devices

        // Input values
        LaRealValue[] inputValuesTemps;
        LaBoolValue[] inputValuesDis;
        LaBoolValue[] outputValuesDos;

        // Output values
        bool jxDo1;


        public event SignalDelegate NewValues;

        public override bool GetNewValue(int chNo, LaValue valRec)
        {
            bool found;

            found = true;

            if (chNo <= cNoOfDis)
            {
                //LaBoolValue bv = (LaBoolValue)valRec;
                var bv = valRec as LaBoolValue;
                bv.FieldValue = inputValuesDis[chNo - 1].FieldValue;
                bv.Status = 192;
            }
            else if (chNo >= cDallasTempFirstCh && chNo <= cDallasTempLastCh)
            {
                LaRealValue rv = (LaRealValue)valRec;
                rv.Value = inputValuesTemps[chNo-cDallasTempFirstCh].Value;
                rv.Status = 192;
            }
            else
                found = false;

            return found;
        }

        public string IP
        {   get { return m_ip; }
            set { if (value != String.Empty)
                    m_ip = value;
            }
        }

        public string XmlFile
        {
            get { return m_xmlFile; }
            set
            {
                if (value != string.Empty)
                    m_xmlFile = value;
                else
                    logger.Error("Empty string value for property: m_xmlFile.");
            }
        }

        // Constructor
        public Barionet(string newName, string newLocation, string newIP, string newXmlFile) : base (newName, newLocation)
        {
            IP = newIP;
            m_location = newLocation;
            XmlFile = newXmlFile;
            m_barionetUrl = "http://" + IP + "/" + XmlFile;  // http string to download data from the Barionet

            inputValuesTemps = new LaRealValue[cNoOfDallasTemps];       // Dallas temperature values
            for (int i = 0; i < cNoOfDallasTemps; i++) inputValuesTemps[i] = new LaRealValue();

            inputValuesDis = new LaBoolValue[cNoOfDis];                 // DI inputs
            for (int i = 0; i < cNoOfDis; i++) inputValuesDis[i] = new LaBoolValue(null);

            outputValuesDos = new LaBoolValue[cNoOfDos];                // DO values
            for (int i = 0; i < cNoOfDos; i++) outputValuesDos[i] = new LaBoolValue(null);

            // Outputs
            CgiIfc = new CgiSetOutput(IP, "rc.cgi");

            logger.Info("Barionet created: " + newName + ", " + newLocation + ", " + newIP + ", " + newXmlFile);
        }



        /// <summary>
        /// Read inputs from the Barionet device.
        /// Input is read from the XML specified in the m_barionetUrl
        /// </summary>
        public void ReadInputs()
        {
            string attribute;       // xml element attribute
            int ch;                 // channel number from attribute index

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(m_barionetUrl);
            webRequest.Timeout = 7000;

            try
            {
                HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();

                //
                //Get a stream to send the web page source
                Stream s = webResponse.GetResponseStream();
                string strContents = new StreamReader(s).ReadToEnd();

                // Write data in received stream to file. This file is read by the WebService for input to Web app
                StreamWriter swrite = new StreamWriter(@"C:\lha\bin\Devices\Barionet\BarioFieldData.xml");
                swrite.Write(strContents);
                swrite.Close();

                // Create the XmlReader object.
                XmlReader xmlReader = XmlReader.Create(new StringReader(strContents));

                // Read Dallas temperatures
                while (xmlReader.Read())
                {
                    // Only detect start elements.
                    if (xmlReader.IsStartElement())
                    {
                        // Get element name and switch on it.
                        switch (xmlReader.Name)
                        {
                            case "temp":
                                // Search for the attribute name on this current node.
                                attribute = xmlReader["addrMapIdx"];                // Input address map index of the Barionet memory => the channel number is derived from this
                                if (attribute != null)
                                {
                                    ch = Convert.ToInt32(attribute) - cDallasTempsBase;
                                    if (xmlReader.ReadToDescendant("value"))
                                    {
                                        if (ch < cNoOfDallasTemps)
                                            inputValuesTemps[ch].Value = xmlReader.ReadElementContentAsDouble();
                                        else
                                            logger.Error("Barionet.ReadInput: Temp 'address map index' attribute translated to illegal input value array index.");
                                    }
                                }
                                else
                                    logger.Error("Barionet.ReadInput: No 'address map index' (addrMapIdx) attribute for temp element.");
                                break;

                            case "di":
                                // Search for the attribute name on this current node.
                                attribute = xmlReader["addrMapIdx"];                // Input address map index of the Barionet memory => the channel number is derived from this
                                if (attribute != null)
                                {
                                    ch = Convert.ToInt32(attribute) - cDiInputBase;
                                    if (xmlReader.ReadToDescendant("value"))
                                    {
                                        if (ch < cNoOfDis)
                                            inputValuesDis[ch].FieldValue = xmlReader.ReadElementContentAsBoolean();
                                        else
                                            logger.Error("Barionet.ReadInput: DI 'address map index' translated to illegal input value array index.");
                                    }
                                }
                                else
                                    logger.Error("Barionet.ReadInput: No 'address map index' (addrMapIdx) attribute for DI element.");
                                break;
                        }
                    }
                }
                s.Close();

                FireNewValues();     // Fire new data event to update input signals

            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
        }

        // Fire event to update input signals
        private void FireNewValues()
        {
            var handler = NewValues;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }



        // ************* EVENT HANDLING ********************
        public virtual void Subscribe(IObservable<LaBoolValue> provider)
        {
            unsubscriber = provider.Subscribe(this);
        }

        public virtual void Unsubscribe()
        {
            unsubscriber.Dispose();
        }

        public virtual void OnCompleted()
        {
            // Do nothing.
        }

        public virtual void OnError(Exception error)
        {
            // Do nothing.
        }

        public virtual void OnNext(LaBoolValue output)
        {
            if(output.ChNo <= cNoOfDos)
            {
                outputValuesDos[output.ChNo].Value = output.Value;
            }
            //Sending commands to Barionet
            CgiIfc.sendCgi(output.ChNo.ToString(), output.Value);    // Send channel value to Barionet, as CgiScript
        }

    }
}
