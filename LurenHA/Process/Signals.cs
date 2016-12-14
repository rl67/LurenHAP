using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using LABaseStuff;

namespace LASignals
{

    /// <summary>
    /// Signal base class./// </summary>
    /// <remarks> Class holds all generic properties and functions for a signal. </remarks>
    class Signal
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private int m_chNo;             // Channel number at the device which the signal is connected to

        /// <summary>
        /// Name of the signal source, "logic" or name of the IO device supplying the signal.
        /// </summary>
        public string SignalSource
        {
            get; set;
        }

        /// <summary>
        /// Signal's channel number at the device providing the signal value./// </summary>
        public int ChNo_
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

        /// <summary>
        /// Tag value for signal. /// </summary>
        public string Tag
        {
            get; set;
        }

        /// <summary>
        /// Signal description. /// </summary>
        public string Description
        {
            get; set;
        }

        /// <summary>
        /// Text to be displayed when signal staus = true. /// </summary>
        public string StatusOnText
        {
            get; set;
        }

        /// <summary>
        /// Text to be displayed when signal status = false./// </summary>
        public string StatusOffText
        {
            get; set;
        }

        /// <summary>
        /// Return Tag + Description of signal, to be used when posting to the event log. /// </summary>
        public string EventIdentity
        {
            get { return Tag + "\t" + Description + "\t"; }
        }

        public virtual void GetValue( ref int achNo, LaValue aValue)
        {

        }

        /// <summary>
        /// Constructor./// </summary>
        public Signal()
        {
            SignalSource = "logic";     // At default signal input source is logic, if not connected to a IO device.
        }
    }

    /// <summary>
    /// SignalAi
    /// </summary>
    class SignalAi : Signal
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public LaRealValue Input;

        public SignalAi()
        {
            Input = new LaRealValue();
        }

        // handler for signal update event
        public void HandleNewInpValue(object sender, EventArgs e)
        {
            LABase obj = (LABase)sender;
            obj.GetNewValue(Input.ChNo, (LaRealValue)Input);
        }
    }

    /// <summary>
    /// SignalDi
    /// </summary>
    class SignalDi : Signal
    {
        private static Logger loggger = LogManager.GetCurrentClassLogger();

        public LaBoolValue Input;

        public SignalDi()
        {
            Input = new LaBoolValue(this.OnValueChanged);   // Pass on event handler to Input signal, bubble Input event up to this object
        }

        // handler for input signal update event. Initiated by Input device
        public void HandleNewInpValue(object sender, EventArgs e)
        {
            LABase obj = (LABase)sender;            // cast to this class
            obj.GetNewValue(Input.ChNo, (LaBoolValue)Input);
        }

        /// <summary>
        /// Returns a the status string matching current value
        /// </summary>
        public string ValueStatusText
        {
            get
            {
                if (Input.Value)
                    return StatusOnText;
                else
                    return StatusOffText;
            }
        }
        // Event raised when value has changed, initiated by .Input
        public void OnValueChanged(object sender, EventArgs e)
        {
            if (e is BoolEventArgs)
            {
                BoolEventArgs be = (BoolEventArgs)e;
            }
        }
    }


    /// <summary>
    /// SignalDO
    /// </summary>
    /// 
    class SignalDo :Signal, IObservable<LaBoolValue>
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        List<IObserver<LaBoolValue>> observers;         // List for storing observers, output channel and UI

        private bool dmy;

        public LaBoolValue Output;

        /// <summary>
        /// Constructor
        /// </summary>
        public SignalDo()
        {
            Output = new LaBoolValue(this.OnValueChanged);  // Assign event handler to Output
            observers = new List<IObserver<LaBoolValue>>();
        }

        public IDisposable Subscribe(IObserver<LaBoolValue> observer)
        {
            if (!observers.Contains(observer))
                observers.Add(observer);

            return new Unsubscriber(observers, observer);
        }

        public void OnValueChanged(object sender, EventArgs e)
        {
            if(e is BoolEventArgs)
            {
                BoolEventArgs be = (BoolEventArgs)e;
                dmy = be.Value;

                // push new value to the observers
                foreach (var observer in observers)
                    observer.OnNext(Output);
            }
        }

        /// <summary>
        /// To be returned to the subscriberst so they can unsubscribe
        /// </summary>
        private class Unsubscriber : IDisposable
        {
            private List<IObserver<LaBoolValue>> _observers;
            private IObserver<LaBoolValue> _observer;

            public Unsubscriber(List<IObserver<LaBoolValue>> observers, IObserver<LaBoolValue> observer)
            {
                this._observers = observers;
                this._observer = observer;
            }

            public void Dispose()
            {
                if (!(_observer == null)) _observers.Remove(_observer);
            }
        }

    }

}
