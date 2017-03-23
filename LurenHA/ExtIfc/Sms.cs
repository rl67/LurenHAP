using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Threading;
using System.Text.RegularExpressions;


namespace LurenHA.ExtIfc
{
    class Sms
    {

        public class clsSMS
        {
            AutoResetEvent receiveNow = new AutoResetEvent(false);

            #region Open and Close Ports
            //Open Port
            public SerialPort OpenPort(string p_strPortName, int p_uBaudRate, int p_uDataBits, int p_uReadTimeout, int p_uWriteTimeout)
            {
               
                SerialPort port = new SerialPort();

                try
                {
                    port.PortName = p_strPortName;                 //COM1
                    port.BaudRate = p_uBaudRate;                   //9600
                    port.DataBits = p_uDataBits;                   //8
                    port.StopBits = StopBits.One;                  //1
                    port.Parity = Parity.None;                     //None
                    port.ReadTimeout = p_uReadTimeout;             //300
                    port.WriteTimeout = p_uWriteTimeout;           //300
                    port.Encoding = Encoding.GetEncoding("utf-8");
                    port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
                    port.Open();
                    port.DtrEnable = true;
                    port.RtsEnable = true;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                return port;
            }

            //Close Port
            public void ClosePort(SerialPort port)
            {
                try
                {
                    port.Close();
                    port.DataReceived -= new SerialDataReceivedEventHandler(port_DataReceived);
                    port = null;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            #endregion

            //Execute AT Command
            public string ExecCommand(SerialPort port, string command, int responseTimeout, string errorMessage)
            {
                try
                {

                    port.DiscardOutBuffer();
                    port.DiscardInBuffer();
                    receiveNow.Reset();
                    port.Write(command + "\r");

                    string input = ReadResponse(port, responseTimeout);
                    if ((input.Length == 0) || ((!input.EndsWith("\r\n> ")) && (!input.EndsWith("\r\nOK\r\n"))))
                        throw new ApplicationException("No success message was received.");
                    return input;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            //Receive data from port
            public void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
            {
                try
                {
                    if (e.EventType == SerialData.Chars)
                    {
                        receiveNow.Set();
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            public string ReadResponse(SerialPort port, int timeout)
            {
                string buffer = string.Empty;
                try
                {
                    do
                    {
                        if (receiveNow.WaitOne(timeout, false))
                        {
                            string t = port.ReadExisting();
                            buffer += t;
                        }
                        else
                        {
                            if (buffer.Length > 0)
                                throw new ApplicationException("Response received is incomplete.");
                            else
                                throw new ApplicationException("No data received from phone.");
                        }
                    }
                    while (!buffer.EndsWith("\r\nOK\r\n") && !buffer.EndsWith("\r\n> ") && !buffer.EndsWith("\r\nERROR\r\n"));
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                return buffer;
            }


            

            #region Send SMS

            static AutoResetEvent readNow = new AutoResetEvent(false);

            public bool sendMsg(SerialPort port, string PhoneNo, string Message)
            {
                bool isSend = false;

                try
                {

                    string recievedData = ExecCommand(port, "AT", 300, "No phone connected");
                    recievedData = ExecCommand(port, "AT+CMGF=1", 300, "Failed to set message format.");
                    String command = "AT+CMGS=\"" + PhoneNo + "\"";
                    recievedData = ExecCommand(port, command, 300, "Failed to accept phoneNo");
                    command = Message + char.ConvertFromUtf32(26) + "\r";
                    recievedData = ExecCommand(port, command, 3000, "Failed to send message"); //3 seconds
                    if (recievedData.EndsWith("\r\nOK\r\n"))
                    {
                        isSend = true;
                    }
                    else if (recievedData.Contains("ERROR"))
                    {
                        isSend = false;
                    }
                    return isSend;
                }
                catch (Exception ex)
                {
                    throw ex;
                }

            }
            static void DataReceived(object sender, SerialDataReceivedEventArgs e)
            {
                try
                {
                    if (e.EventType == SerialData.Chars)
                        readNow.Set();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            #endregion

            #region Delete SMS
            public bool DeleteMsg(SerialPort port, string p_strCommand)
            {
                bool isDeleted = false;
                try
                {

                    #region Execute Command
                    string recievedData = ExecCommand(port, "AT", 300, "No phone connected");
                    recievedData = ExecCommand(port, "AT+CMGF=1", 300, "Failed to set message format.");
                    String command = p_strCommand;
                    recievedData = ExecCommand(port, command, 300, "Failed to delete message");
                    #endregion

                    if (recievedData.EndsWith("\r\nOK\r\n"))
                    {
                        isDeleted = true;
                    }
                    if (recievedData.Contains("ERROR"))
                    {
                        isDeleted = false;
                    }
                    return isDeleted;
                }
                catch (Exception ex)
                {
                    throw ex;
                }

            }
            #endregion

        }
    }

}
