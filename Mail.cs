using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net;

namespace ServerWorker
{
    class Mail
    {
        public static void sendMail (string adress, string subject, string message) {

            using (var client = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(Secret.Email, Secret.EmailPassword),
                EnableSsl = true
            })
            {
                try
                {
                    client.Send(Secret.Email, adress, subject, message);
                }
                catch (Exception)
                {
                    System.Diagnostics.Debug.WriteLine("E-mail format was not correct.");
                }
            }
        }
    }
}