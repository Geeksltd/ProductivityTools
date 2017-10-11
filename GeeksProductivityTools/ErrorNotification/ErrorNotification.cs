using Geeks.GeeksProductivityTools.Menus.Cleanup;
using System;
using System.IO;

namespace Geeks.GeeksProductivityTools
{
    internal static class ErrorNotification
    {
        internal static void EmailError(string message)
        {
        }

        static object logObj = new object();
        internal static void LogError(Exception exp)
        {
            Log(exp.ToString() + "\r\n" + exp.StackTrace + "\r\n+++++++++++++++++++++++++++++++++++\r\n");
        }

        internal static void LogError(Exception exp, EnvDTE.ProjectItem projectItem)
        {
            Log(projectItem.ToFullPathPropertyValue() + " : \r\n" + exp.ToString() + "\r\n" + exp.StackTrace + "\r\n+++++++++++++++++++++++++++++++++++\r\n");
        }
        private static void Log(string message)
        {
            lock (logObj)
            {
                File.AppendAllText("log.txt", DateTime.Now.ToString() + "\r\n" + message);
            }
        }

        internal static void EmailError(Exception ex)
        {
            // TODO: Add feature to Settings for sending the error log to the dev team if requested by end users

            //var message = new MailMessage("Geeks.Productivity.Tools@gmail.com",
            //                              "ali.ashoori@geeks.ltd.uk",
            //                              $"Error from Geeks Productivity Tools {DateTime.Now.Date}", GenerateErrorBody(ex))
            //{
            //    IsBodyHtml = true
            //};

            //var client = new SmtpClient("smtp.gmail.com", 587)
            //{
            //    Credentials = new NetworkCredential("Geeks.Productivity.Tools@gmail.com", "Espresso123"),
            //    EnableSsl = true
            //};
            //client.Send(message);
        }

        static string GenerateErrorBody(Exception e)
        {
            return "<span style='font-weight: bold'>Message:</span>" + "<p>" + e.Message + "</p>" + "</br>" +
                   "<span style='font-weight: bold'>Inner exception message:</span>" + "<p>" + e.InnerException?.Message + "</p>" + "</br>" +
                   "<span style='font-weight: bold'>Stack Trace:</span>" + "<p>" + e.StackTrace + "</p>";
        }
    }
}
