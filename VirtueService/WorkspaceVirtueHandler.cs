using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Security.Cryptography;
using System.Configuration;
using System.Management.Automation;
using System.Collections.ObjectModel;
using System.Threading;
using SimpleImpersonation;

namespace VirtueService
{
    class WorkspaceVirtueHandler
    {
        public BlockingCollection<VirtueConfigurationEvent> queue = new BlockingCollection<VirtueConfigurationEvent>();
        String username = null;
        static string serverUrl = "https://ip-172-16-0-36.ec2.internal"; //kops and C&C server, REST endpoint
        static string getPath = "/remoteapp";
        static string log = @"C:\Users\Public\Documents\virtue.txt";
        static VirtueKeygen key = null;
        string remoteAppSkeleton = null;
        static string tokenFile = "C:\\virtue_private_token.txt";
        static string userPrivateToken = null;
        static WebRequestHandler handler = new WebRequestHandler();

        public static void LoadUserToken()
        {
            if (File.Exists(tokenFile) && userPrivateToken == null)
            {
                userPrivateToken = File.ReadAllText(tokenFile).Trim();
                WriteLog("Loaded user's token from file. " + Environment.NewLine);
            }

            if (!File.Exists(tokenFile))
            {
                userPrivateToken = Guid.NewGuid().ToString();
                File.WriteAllText(tokenFile, userPrivateToken);
            }
            else if (userPrivateToken == null)
            {
                userPrivateToken = File.ReadAllText(tokenFile).Trim();
            }
        }

        public static void WriteLog(string message)
        {
            try
            {
                File.AppendAllText(log, message);
            }
            catch (Exception e) { }
        }

        public void Enqueue(VirtueConfigurationEvent evt)
        {
            if (evt.ConfigurationEvent == VirtueConfigurationEvent.VirtueEvent.POLL)
            {
                if (queue.Count == 0)
                {
                    WriteLog("Adding POLL request to event queue." + Environment.NewLine);
                    queue.Add(evt);
                }
            }
            else
            {
                queue.Add(evt);
                WriteLog("Adding " + evt.ConfigurationEvent.ToString() + " request to event queue." + Environment.NewLine);
            }
        }
        
        public string GetPowershellSkeleton()
        {
            var appSettings = ConfigurationManager.AppSettings;
            string file = appSettings["PowershellSkeleton"];
            try
            {
                return File.ReadAllText(file);
            } catch (Exception e) {
                return null;
            }
        }
        
        public void PollVirtueCommandControl(System.ComponentModel.BackgroundWorker worker,
            System.ComponentModel.DoWorkEventArgs e)
        {
            handler.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            remoteAppSkeleton = GetPowershellSkeleton();

            while (true)
            {
                VirtueConfigurationEvent logonEvent = queue.Take();
                PollCommandControl();
            }
        }

        public void PollCommandControl()
        {
            WriteLog("Polling virtue C&C REST endpoint to get remoteapp configs." + Environment.NewLine);
            Task<List<String>> conf = GetRemoteAppConfiguration(getPath);
            List<String> powershellScripts = conf.Result;
            if (powershellScripts != null && powershellScripts.Count > 0)
            {
                WriteLog("Configuring remoteapp host. About to call ConfigureRemoteApp()..." + Environment.NewLine);
                ConfigureRemoteApp(powershellScripts);
            }
            else
            {
                WriteLog("Did not find any remoteapp configurations..." + Environment.NewLine);
            }
        }

        static string GetDesktop(string username)
        {

            if (Directory.Exists("C:\\Users\\" + username + "\\Desktop\\"))
            {
                return "C:\\Users\\" + username + "\\Desktop\\";
            }
            if (Directory.Exists("D:\\Users\\" + username + "\\Desktop\\"))
            {
                return "D:\\Users\\" + username + "\\Desktop\\";
            }

            if (Directory.Exists("C:\\Users\\"))
            {
                string[] dirs = Directory.GetDirectories("D:\\Users\\");
                if (dirs != null && dirs.Length > 0)
                    return dirs[0] + "\\Desktop\\";
            }

            if (Directory.Exists("D:\\Users\\"))
            {
                string[] dirs = Directory.GetDirectories("D:\\Users\\");
                if (dirs != null && dirs.Length > 0)
                    return dirs[0] + "\\Desktop\\";
            }


            return null;
        }

        static void runPS(string psscript)
        {
            using (PowerShell PowerShellInstance = PowerShell.Create())
            {
                WriteLog("Running the following powershell script: " + Environment.NewLine + psscript + "_________________________" + Environment.NewLine + Environment.NewLine);
                // use "AddScript" to add the contents of a script file to the end of the execution pipeline.
                // use "AddCommand" to add individual commands/cmdlets to the end of the execution pipeline.
                PowerShellInstance.AddScript(psscript);

                // invoke execution on the pipeline (collecting output)
                Collection<PSObject> PSOutput = PowerShellInstance.Invoke();

                // loop through each output object item
                foreach (PSObject outputItem in PSOutput)
                {
                    // if null object was dumped to the pipeline during the script then a null
                    // object may be present here. check for null to prevent potential NRE.
                    if (outputItem != null)
                    {
                        WriteLog("Configured remoteapp got result: " + outputItem.ToString() + Environment.NewLine);
                    }
                }

                PSDataCollection<ErrorRecord> errs = PowerShellInstance.Streams.Error;
                foreach (ErrorRecord currErr in errs)
                {
                    if (currErr != null)
                        WriteLog("Powershell error: " + currErr.ToString() + Environment.NewLine);
                }
            }
        }

        static void ConfigureRemoteApp(List<String> scripts)
        {
            WriteLog("called configureremoteapp." + Environment.NewLine);
            foreach (String psscript in scripts)
            {
                Thread.Sleep(10000); //For some reason the powershell scripts output erratically for RDS deployments...
                try
                {
                    if (scripts.IndexOf(psscript) == scripts.Count - 1)
                    {
                        WriteLog("attempting impersonation of the virtue domain administrator account.");
                        using (Impersonation.LogonUser("VIRTUE", "Administrator", "DoingItL1v3", LogonType.Network))
                        {
                            WriteLog("Impersonated the virtue domain administrator account.");
                            runPS(psscript);
                        }
                    }
                    else
                    {
                        WriteLog("Running script without impersonation...");
                        runPS(psscript);
                    }
                } catch (Exception e) {
                    WriteLog("Failed to impersonate the virtue domain administrator account.");
                    WriteLog("error: " + e.ToString() + Environment.NewLine);
                }
                
            }
        }

        static HttpClient GetHttpClient(string serverUrl)
        {
          
            HttpClient httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(serverUrl),
                Timeout = TimeSpan.FromSeconds(10)
            };

            return httpClient;
        }

        static async Task<List<String>> GetRemoteAppConfiguration(string path)
        {
            HttpClient httpClient = GetHttpClient(serverUrl);
            WriteLog("Getting user virtue config from C&C server." + Environment.NewLine);
            String host = System.Net.Dns.GetHostName();
            List<String> conf = null;
            LoadUserToken();

            try
            {
                HttpResponseMessage response = await httpClient.PostAsync(path, new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("host", host),
                    new KeyValuePair<string, string>("token", userPrivateToken)
                }));

                if (response.IsSuccessStatusCode)
                {
                    string res = await response.Content.ReadAsStringAsync();
                    JavaScriptSerializer JSserializer = new JavaScriptSerializer();
                    var virtues = JSserializer.Deserialize<String[]>(res);
                    if (virtues != null)
                    {
                        conf = virtues.ToList<String>();
                        WriteLog("Polling GetRemoteAppConfiguration: found virtues to configure." + Environment.NewLine);
                    }
                    else
                    {
                        WriteLog("Polling GetRemoteAppConfiguration: no virtues to configure." + Environment.NewLine);
                    }
                    
                }
                 WriteLog("Polling GetRemoteAppConfiguration C&C REST endpoint got HTTP status code " + response.StatusCode + Environment.NewLine);
            }
            catch (Exception e)
            {
                 WriteLog("Exception in GetUserVirtueConfiguration: " + e.Message + Environment.NewLine);
                 WriteLog("Exception in GetUserVirtueConfiguration: " + e.StackTrace + Environment.NewLine);
            }

            return conf;
        }
    }
}
