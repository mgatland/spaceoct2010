using System;
using System.Net;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
#if WINDOWS_PHONE
using Microsoft.Devices;
#endif

/*
 * Send a web request POST to the analytics tracking website.
 * Ignore the response.
 * 
 * For now we're not sending any very sophisticated data over.
 * 
 */
namespace SpaceOctopus.Data
{
    class TrackedEventPersistedData
    {
        #region singleton
        private static TrackedEventPersistedData instance;
        public static TrackedEventPersistedData Instance
        {
            get {
                if (instance == null) instance = new TrackedEventPersistedData();
                return instance;
            }
        }
        #endregion
        System.Collections.Generic.
        Dictionary<string, bool> alreadySentValues; //actually just a set. TODO: replace with Hashset.
        private const string filename = "tracking-already-sent.txt";
        private int version; //file version

        private TrackedEventPersistedData()
        {
            alreadySentValues = new Dictionary<string, bool>();
            Action<StreamReader> handler = delegate(StreamReader reader)
            {
                int fileVersion = IOUtil.ReadInt(reader);
                //Debug.Assert(version == 1);
                while (!reader.EndOfStream)
                {
                    alreadySentValues.Add(reader.ReadLine(), true);
                }
            };
            IOUtil.ReadFile(filename, handler, IOUtil.NothingAction);
        }

        private void Save()
        {
            Action<StreamWriter> handler = delegate(StreamWriter writer)
            {
                Debug.WriteLine("Saving alreadysent keys:");
                writer.WriteLine(version);
                foreach (string s in alreadySentValues.Keys) 
                {
                    writer.WriteLine(s);
                    Debug.WriteLine(s);
                }
                Debug.WriteLine("");
            };

            IOUtil.WriteFile(filename, handler, IOUtil.NothingAction);
        }

        public bool HaveISent(string value)
        {
            return alreadySentValues.ContainsKey(value);
        }

        public void AddValue(string value)
        {
            alreadySentValues.Add(value, true);
            Save();
        }
    }

    class TrackedEvent
    {
        string code;
        string data;
        bool oneShot;

        public TrackedEvent(string code, string data, bool oneShot)
        {
            Debug.Assert(data == "", "TrackedEvent does not support data.");
            this.code = code;
            this.data = data;
            this.oneShot = oneShot;
        }

        public bool Fire()
        {
#if WINDOWS_PHONE
            if (oneShot && TrackedEventPersistedData.Instance.HaveISent(code))
            {
                Debug.WriteLine("Tracking message '" + code + "' ignored, it has been sent before.");
                return false;
            }
            try
            {
                string gameString;
                if (Microsoft.Devices.Environment.DeviceType == Microsoft.Devices.DeviceType.Emulator)
                {
                    gameString = "som-wp7-emulator";
                }
                else
                {
                    gameString = "som-wp7";
                }
                Debug.WriteLine("Tracking message: '" + code + "'");
                HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create("http://www.newnorthroad.com/tracking/?game=" + gameString + "&code=" + code);
                httpWebRequest.Method = "POST";
                httpWebRequest.ContentType = "text/html";

                // Set the content type of the data being posted.
                //httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                httpWebRequest.BeginGetRequestStream(RequestStream_Completed, httpWebRequest);

            }
            catch (Exception e)
            {
                Debug.WriteLine("Error in tracked event request: ");
                Debug.WriteLine(e.Message);
            }
            return true;
#endif
#if WINDOWS
          return false; //FIXME
#endif
        }

        void RequestStream_Completed(IAsyncResult result)
        {
            try
            {
                Debug.WriteLine("Got the stream, time to send");
                HttpWebRequest request = (HttpWebRequest)result.AsyncState;
                Stream newStream = request.EndGetRequestStream(result);

                byte[] byte1 = IOUtil.StringToAscii(data);

                newStream.Write(byte1, 0, byte1.Length);
                // Close the Stream object.
                newStream.Close();
                request.BeginGetResponse(Response_Completed, request);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error in RequestStream_Completed: ");
                Debug.WriteLine(e.Message);
            }
        }

        void Response_Completed(IAsyncResult result)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)result.AsyncState;
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(result);
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    string xml = streamReader.ReadToEnd();
                    using (XmlReader reader = XmlReader.Create(new StringReader(xml)))
                    {
                        reader.MoveToContent();
                        //reader.GetAttribute(0);
                        //reader.MoveToContent();
                        string message = reader.ReadInnerXml(); //we don't actually use the response.
                        Debug.WriteLine("Tracking response: " + message);
                        if (oneShot)
                        {
                            //This one-shot message was received, make sure it will never be sent again.
                            TrackedEventPersistedData.Instance.AddValue(code);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error in TrackedEvent response:");
                Debug.WriteLine(e.Message);
            }
        }
    }
}
