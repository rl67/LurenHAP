using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net;
using NLog;


namespace ExtIfc
{
    static class SendMail
    {
        public static void Sys10_SystemFault()
        {

            //    /// Created,
            //    /// Local dependency, Microsoft .Net framework
            //    /// Description, Send an email using (SMTP).
            //    ///
             
            Logger logger = LogManager.GetCurrentClassLogger();

            MailMessage objMailMessage = new MailMessage();
            System.Net.NetworkCredential objSMTPUserInfo = new System.Net.NetworkCredential();
            SmtpClient objSmtpClient = new SmtpClient();

            try
            {
                objMailMessage.From = new MailAddress("rolf@kviteluren67.net");
                objMailMessage.To.Add("rolf67@gmail.com");
                //objMailMessage.To.Add(new MailAddress("helge.haga@wartsila.com"));
                //objMailMessage.To.Add(new MailAddress("ekgrov@online.no"));
                objMailMessage.Subject = "Kloakkpume systemfeil!";
                objMailMessage.Body = 
                    @"Mulige feilkilder: 
                    Feil i styringsskap:
                    1. Skap manglar 230V
                    2. Feil på 24 V side i skap
                    3. Termisk motorvern for pumpe aktivert

                    Kloakktank:
                    Høgt septiknivå i kloakktank. 
                    (Tank fylles opp, og går i overløp. Feil bør utbetrast snarast!)";

                objSmtpClient = new SmtpClient("smtp.altibox.no", 25); /// Server IP
                objSMTPUserInfo = new System.Net.NetworkCredential("rolf@kviteluren67.net", "wasma337");
                objSmtpClient.UseDefaultCredentials = false;
                objSmtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                objSmtpClient.Send(objMailMessage);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                throw ex;
            }

            finally
            {
                objMailMessage = null;
                objSMTPUserInfo = null;
                objSmtpClient = null;
            }
        }
    }
}
