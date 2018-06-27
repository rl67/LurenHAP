using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace LABaseStuff
{
    // Delegate for new signal value


    /// <summary>
    //  Evnet declaration setcion
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>

    public delegate void SignalDelegate(object sender, EventArgs e);

    /// <summary>
    /// BoolEventArgs
    /// </summary>
    public class BoolEventArgs : EventArgs
    {
        public BoolEventArgs(bool bVal)
        {
            this.m_bVal = bVal;
        }

        public int ChNo
        {
            get { return m_chNo; }
        }

        public bool Value
        {
            get { return m_bVal; }
        }

        private bool m_bVal;
        private int m_chNo;
    }



    /// <summary>
    /// LaValue 
    /// </summary>
    public class LaValue
    {
        // Properties
        private Int32 m_status;
        private int m_chNo;             // Channel number at the device which the signal is connected to
        private bool m_forced;          // Forced state of input signal.

        // Constants
        private const int cForced = 0xD0;   // OPC status for forced????????????????

        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Signal's channel number at the device providing the signal value./// </summary>
        public int ChNo
        {
            get { return m_chNo; }
            set
            {
                if (value >= 0)
                    m_chNo = value;
                else
                    logger.Error("Property value has to be equal to or greather than zero: chNo.");
            }
        }

        public LaValue()
        {
            m_status = 0x0;     // Find the status BAD, uninitialized.
        }

        public int Status
        {
            get { return m_status; }
            set { m_status = value; }
        }

        public bool Forced
        {
            get { return m_forced; }
            set
            {
                if (value)
                {
                    m_status |= cForced;
                    //todo Add event for Forced on/OFF
                }
                else
                {
                    m_status &= ~cForced;
                }

                m_forced = value;
            }
        }
    }

    /// <summary>
    /// LaRealValue
    /// </summary>
    public class LaRealValue : LaValue
    {
        private double m_rVal;

        public double Value
        {
            get { return m_rVal; }
            set { m_rVal = value; }
        }

    }

    /// <summary>
    /// LaBoolValue
    /// </summary>
    public class LaBoolValue : LaValue
    {
        private bool m_bVal;                    // Application value
        private bool m_fieldVal;                // Field value

        public event SignalDelegate Changed;    // event fired when value changed.

        /// <summary>
        /// LaBoolValue event delegate
        /// </summary>
        /// <param name="evHandler"></param>
        public LaBoolValue(SignalDelegate evHandler)
        {
            Changed += (SignalDelegate)evHandler;
        }

        /// <summary>
        /// Field value property
        /// </summary>
        public bool FieldValue
        {
            get { return m_fieldVal; }
            set
            {
                if (value != m_fieldVal)
                {
                    m_fieldVal = value;
                    BoolEventArgs ev = new BoolEventArgs(m_fieldVal);
                    FireEventChanged(ev);

                    if(!Forced)
                    {
                        Value = value;
                    }
                }
            }
        }

        /// <summary>
        /// Value property
        /// </summary>
        public bool Value
        {
            get { return m_bVal; }
            set
            {
                if(value != m_bVal)
                {
                    m_bVal = value;
                }
            }
        }

        /// <summary>
        /// FireEventChanged Raise Changed event
        /// </summary>
        /// <param name="e"></param>
        private void FireEventChanged(EventArgs e)
        {
            var handler = Changed;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }

    /// <summary>
    /// LABase: Base class for devices. Devices to be inerited from this.
    /// </summary>
    class LABase
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private string m_name;
        private string m_location;

        public string Name
        {
            get { return m_name; }
            set
            {
                if (value != string.Empty)
                    m_name = value;
                else
                    logger.Error("Empty string value for property: Name.");
            }
        }
        public string Location
        {
            get { return m_location; }
            set
            {
                if (value != string.Empty)
                    m_location = value;
                else
                    logger.Error("Empty string value for property: Location.");
            }
            
        }

        //        if(string.IsNullOrEmpty(text))
        //          throw new ArgumentException("message", "text");
        //        If you are using .NET 4 you can do this. Of course this depends on whether you consider a string that contains only white space to be invalid.

        //if (string.IsNullOrWhiteSpace(text))
        //    throw new ArgumentException("message", "text");
        //ArgumentNullException


        public LABase(string newName, string newLocation)
        {
            Name = newName;
            Location = newLocation;
        }

        public virtual bool GetNewValue(int chNo, LaValue valRec)
        {
            return true;
        }


    }
}
