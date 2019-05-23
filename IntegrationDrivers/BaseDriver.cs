using Common.Models;
using IntegrationDrivers.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace IntegrationDrivers
{
    public abstract class BaseDriver
    {
        private static string deviceIpAddress;
        private static string devicePort;
        private string volumeUnits;
        public Socket sock;
        private static bool m_connected;
        private readonly UDISettings _udiSettings;
        private RegisterHeadSettings _registerHeadSettings;

        public List<Prompts> prompts = new List<Prompts>();
        public List<TagModel> tagList = new List<TagModel>();

        //loadStatus lets the program know at what stage in the loading progress the ML is in
        // 0 = Idle (sends the first prompt)
        // 1 = In prompts (sends subsequent prompts)
        // 2 = Authorized (sends authorize command)
        // 3 = Complete (populate the volume information and reset status to 0
        public int loadStatus = 0;
        public int promptStep = 0;
        public int totalPromptCount = 0;



        public BaseDriver()
        {
            //_udiSettings = udiSettings.Value;
            //_registerHeadSettings = registerHeadSettings.Value;
            sock = socket();
        }

        public virtual bool Connect(string ipAddress, string armAddress, string port)
        {

            if (!(m_connected))
            {

                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), Convert.ToInt32(port));

                try
                {
                    sock.Connect(endPoint);
                    m_connected = true;
                }
                catch (ObjectDisposedException od)
                {
                    sock = socket();
                    sock.Connect(endPoint);
                    m_connected = true;
                }
                catch (Exception e)
                {
                    m_connected = false;
                }
                //return m_connected;
            }

            return m_connected;

        }

        public virtual bool Disconnect()
        {
            try
            {
                if (!(m_connected))
                    return m_connected;

                //SendCommand("DA");
                sock.Close();
                m_connected = false;
            }
            catch (Exception e)
            {
                m_connected = false;
            }
            return m_connected;
        }

        Socket socket()
        {
            return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public string SendCommand(string command)
        {
            byte[] sendData;

            byte[] receiveData = new byte[1024];
            int rec = 0;

            sendData = Encoding.ASCII.GetBytes("\x02" + "01" + command + "\x03" + "\x03");
            sock.Send(sendData, 0, sendData.Length, 0);
            rec = sock.Receive(receiveData);
            Array.Resize(ref receiveData, rec);
            string statusResultString = Encoding.ASCII.GetString(receiveData, 0, receiveData.Length);
            return statusResultString;
        }

        public void LoadTags(List<TagModel> oasTags)
        {
            tagList = oasTags;
        }

        public void LoadPrompts(List<Prompts> oasPrompts)
        {
            prompts = oasPrompts;
            totalPromptCount = prompts.Count();


            if (totalPromptCount > 0)
            {
                loadStatus = 0;

                var firstPrompt = prompts.Where(x => x.Order == 1).First();
                var result = SendFirstPrompt(firstPrompt.Prompt, firstPrompt.PromptLength);
                if (result.Contains("OK"))
                {
                    promptStep = 1;
                    //updateStatus("Idle");
                    var updFirstPrompt = tagList.Where(x => x.TagName == firstPrompt.TagName).First();
                    if (updFirstPrompt != null)
                    {
                        updFirstPrompt.LastRead = DateTime.Now;
                        updFirstPrompt.Value = 0;
                    }
                }
            }

        }

        public string SendFirstPrompt(string promptValue, string promptLength)
        {
            var result = SendCommand("WP 000 " + promptValue + "&" + promptLength);
            return result;
        }

        public virtual string SendNextPrompt(string promptValue, string promptLength)
        {
            var result = SendCommand("WA 000 " + promptValue + "&" + promptLength);
            return result;
        }

        public virtual string DeviceStatus()
        {
            var result = SendCommand("RS");
            return result;
        }

        public void resetTagModel()
        {
            var resetTags = tagList;
            foreach (var t in tagList)
            {
                t.LastRead = DateTime.Now;
                t.Value = 0;
            }
        }

        public void updateStatus(string status)
        {
            var statusModel = tagList.Where(x => x.TagName.Contains("Status")).First();
            statusModel.Value = status;
            statusModel.LastRead = DateTime.Now;
        }

        public List<TagModel> UpdateModel(List<TagModel> model)
        {
            return model;
        }

        public List<TagModel> ReturnCurrentTaglist()
        {
            return tagList;
        }

        public Prompts getPromptTag(int promptStep)
        {
            if (promptStep > totalPromptCount)
                return null;

            var promptTag = prompts.Where(x => x.Order == promptStep).First();
            return promptTag;
        }

        public TagModel getTag(string tagName)
        {
            var list = tagList.Where(x => x.TagName == tagName).First();

            return list;
        }
    }
}
