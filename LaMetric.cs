using log4net;
using Newtonsoft.Json;
using SmartPesa.Objects;
using SmartPesa.WorkflowLibrary;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace SmartPesa.Workflow
{
    public class LaMetric : DestinationBase
    {
        private static readonly ILog _log = LogManager.GetLogger("LogFile");
        private string _url = "";
        private string _accessToken = "";
        private string _icon = null;
        private int _index = 0; 
        private int _interval = 0;
        private Int64 _txnCounter = 0;
        private DateTime _lastRequest;

        /*
          spWorkflow.spring
          =================
          <objects> 
            <object name="lametric" type="SmartPesa.Workflow.LaMetric,LaMetric"></object>
          </objects> 
          
          spWorkflow.config
          =================
          <Workflow>
            <Destinations>
              <add key="lametric" type="LaMetric" active="true" subscriber="true" />
            </Destinations>
          </Workflow>
         
          <LaMetric>
            <requestSettings>
              <add key="url" value="https://developer.lametric.com/api/V1/dev/widget/update/com.lametric.[unique-uri]" />
              <add key="accessToken" value="[secret]" />
              <add key="icon" value="i59" />
              <add key="index" value="0" />
              <add key="interval" value="60" /> <!-- seconds -->
            </requestSettings>
          </LaMetric>
        */

        public LaMetric()
        {
            _log.Info(" -> Loading LaMetric settings");
            NameValueCollection requestSettings = ConfigurationManager.GetSection("LaMetric/requestSettings") as NameValueCollection;

            if (requestSettings != null)
            {
                _url = requestSettings["url"];
                _accessToken = requestSettings["accessToken"];
                _icon = requestSettings["icon"];
                _index = Convert.ToInt32(requestSettings["index"]);
                _interval = int.Parse(requestSettings["interval"]);
                _lastRequest = DateTime.UtcNow.AddSeconds(-_interval);
            }
            else
            {
                _log.Error("Check configuration LaMetric/requestSettings");
            }
        }

        public override string Name()
        {
            return "LaMetric";
        }

        public override string Version()
        {
            return "1.0";
        }

        public override object ProcessMessage(object payload)
        {
            Payment newPayment = JsonConvert.DeserializeObject<Payment>((string)payload);
            _log.InfoFormat("     {0}{1}{2}", newPayment.TransactionRef, newPayment.Amount.ToString().PadLeft(20).PadRight(40), newPayment.ResponseCode);
            if (newPayment.ResponseCode == ResponseCodes.Approved)
            {
                if (_txnCounter >= 9999999 || DateTime.UtcNow.Day != _lastRequest.Day)
                {
                    _lastRequest = DateTime.UtcNow;
                    _txnCounter = 0;
                }

                _txnCounter++;

                if ((DateTime.UtcNow - _lastRequest).TotalSeconds > _interval)
                {
                    _lastRequest = DateTime.UtcNow;
                    SendRequest();
                }
            }
            return null;
        }

        private async void SendRequest()
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_url);
                request.Accept = "application/json";
                request.Headers.Add("X-Access-Token", _accessToken);
                request.Headers.Add("Cache-Control", "no-cache");
                request.Method = "POST";
                request.ContentType = "application/json; encoding='utf-8'";

                request.ServerCertificateValidationCallback = delegate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };
                StreamWriter streamWriter = new StreamWriter(request.GetRequestStream());
                Frames frames = new Frames(_txnCounter, _icon, _index);
                string requestBody = JsonConvert.SerializeObject(frames);

                streamWriter.Write(requestBody);
                streamWriter.Close();
                streamWriter.Dispose();
                HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse;
                _log.InfoFormat("Response: {0} - {1}", response.StatusCode, response.StatusDescription);
            }
            catch (Exception ex)
            {
                _log.Warn("Response: " + ex.Message);
            }
        }

        public override string Shutdown()
        {
            return null;
        }

        public class Frames
        {
            public List<dynamic> frames { get; set; }

            public Frames(Int64 txnCounter, string icon, int index)
            {
                frames = new List<dynamic>
                {
                    new { text = txnCounter.ToString(), icon = icon, index = index}
                };
            }
        }
    }
}