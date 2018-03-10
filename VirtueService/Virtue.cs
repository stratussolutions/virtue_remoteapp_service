using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VirtueService
{
    public partial class Virtue : ServiceBase
    {
        private System.ComponentModel.BackgroundWorker iconConfigWorker = new BackgroundWorker();
        WorkspaceVirtueHandler handler = null;

        public Virtue()
        {
            InitializeComponent();
            this.CanHandleSessionChangeEvent = true;
        }

        protected override void OnStart(string[] args)
        {
            InitializeBackgroundWorker();
            File.AppendAllText(@"C:\Users\Public\Documents\virtue.txt", "The Virtue service has started." + Environment.NewLine);
            //File.AppendAllText(@"C:\Users\Public\Documents\virtue.txt", "The user private token is " + userPrivateToken + Environment.NewLine);
            handler = new WorkspaceVirtueHandler();
            iconConfigWorker.RunWorkerAsync(handler);

            // Start a thread that calls a parameterized static method.
            Thread newThread = new Thread(EnqueuePollEvent);
            newThread.Start(handler);
        }

        public static string GetProperty(string key)
        {
            try
            {
                System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                return config.AppSettings.Settings[key].Value;
            } catch (Exception e)
            {
                return null;
            }
        }

        public static void SetProperty(string key, string value)
        {
            System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings[key].Value = value;
            config.Save(ConfigurationSaveMode.Modified);
        }

        public static void EnqueuePollEvent(object workspaceVirtueHandler)
        {
            WorkspaceVirtueHandler handler = (WorkspaceVirtueHandler)workspaceVirtueHandler;
            while (true)
            {
                Thread.Sleep(10000);
                handler.Enqueue(new VirtueConfigurationEvent("", VirtueConfigurationEvent.VirtueEvent.POLL));
            }
        }

        private void InitializeBackgroundWorker()
        {
            this.iconConfigWorker.DoWork +=
                new DoWorkEventHandler(BackgroundWorker1_DoWork);
            this.iconConfigWorker.RunWorkerCompleted +=
                new RunWorkerCompletedEventHandler(
            BackgroundWorker1_RunWorkerCompleted);
            this.iconConfigWorker.ProgressChanged +=
                new ProgressChangedEventHandler(
            BackgroundWorker1_ProgressChanged);
        }

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            // Get the BackgroundWorker object that raised this event.  
            System.ComponentModel.BackgroundWorker worker;
            worker = (System.ComponentModel.BackgroundWorker)sender;
            WorkspaceVirtueHandler virtueHandler = (WorkspaceVirtueHandler)e.Argument;
            virtueHandler.PollVirtueCommandControl(worker, e);
        }

        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // This event handler is called when the background thread finishes.  
            // This method runs on the main thread.  
        }

        private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // This method runs on the main thread.  
            
        }

        protected override void OnStop()
        {
        }
    }
}
