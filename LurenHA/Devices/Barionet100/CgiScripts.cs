using System;
using System.Net;


namespace CgiScripts
{
    class CgiSetOutput
    {
        private string IPaddr;
        private string cgiScript;
        private string UrlBase;

        public string cgiResponseStatus;

        public CgiSetOutput(string IP, string script)
        {
            IPaddr = IP;
            cgiScript = script;
            UrlBase = "http://" + IPaddr + "/" + cgiScript + "?o=";
        }

        public void sendCgi( string OutputAddr, bool OutputState )
        {
            HttpWebRequest request;
            HttpWebResponse response;
            
            string RelayState;

            if (OutputState)
            {
                RelayState = "1";
            }
            else
            {
                RelayState = "0";
            }

            string url = UrlBase + OutputAddr + "," + RelayState;
            
            request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 1000;

            try
            {
                response = (HttpWebResponse)request.GetResponse();
                cgiResponseStatus = response.StatusCode.ToString();
                response.Close();
            }
            catch (Exception ex)
            {
                cgiResponseStatus = ex.Message;
            }
        }
    }
}


/*
    void CgiSetOutput( string cgiUrlBase, string Output, string OutputState )
    {
        HttpWebRequest request;
        HttpWebResponse response;

        string strServer;
        string RelayState;

        if ( cbRele1.Checked )
        {
            RelayState = "1";
        }
        else
        {
            RelayState = "0";
        }

        string url = "http://172.16.80.20/rc.cgi?o=1," + RelayState;
        
        request = (HttpWebRequest)WebRequest.Create(url);
        request.Timeout = 1000;

        try
        {
            response = (HttpWebResponse)request.GetResponse();
            strServer = "status:" + response.StatusCode;
            response.Close();
            textBox1.Text = textBox1.Text + strServer;
        }
        catch (Exception ex)
        {
            strServer = ex.Message;
            textBox1.Text = textBox1.Text + strServer;
        }                
    }*/