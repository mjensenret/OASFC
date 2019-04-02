using IntegrationDrivers.Models;
using OASDriverInterface;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace IntegrationDrivers
{
    public class MicroloadUtilities
    {
        Socket sock;
        string mlIpAddress;
        string mlUnits;
        string mlPortNumber;
        string mlArmAddress;
        private bool m_inStatusTimer = false;
        private Timer statusTimer;
        private Timer dynamicDisplayTimer;
        private bool m_connected;
        private bool Initialized = false;
        private List<Prompts> prompts = new List<Prompts>();
        private List<TagModel> tagList = new List<TagModel>();

        //loadStatus lets the program know at what stage in the loading progress the ML is in
        // 0 = Idle (sends the first prompt)
        // 1 = In prompts (sends subsequent prompts)
        // 2 = Authorized (sends authorize command)
        // 3 = Complete (populate the volume information and reset status to 0
        private int loadStatus = 0;
        private int promptStep = 0;
        private int totalPromptCount = 0;

        public enum DataPoint
        {
            Status = 0,
            Prompt = 1,
            TransactionNum = 2,
            Time = 3,
            Volume = 4,
            Totalizer = 5,
            TransactionAvgs = 6
        }

        public enum MeasurementType
        {
            IV = 0,
            Gross = 1,
            GST = 2,
            GSV = 3,
            Mass = 4
        }
        
        public MicroloadUtilities()
        {
            sock = socket();
            statusTimer = new Timer(StatusTimerRoutine, null, Timeout.Infinite, Timeout.Infinite);
            dynamicDisplayTimer = new Timer(DynamicDisplayTimer, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void LoadPrompts(List<Prompts> oasPrompts)
        {
            prompts = oasPrompts;
            totalPromptCount = prompts.Count();

            
            if( totalPromptCount > 0)
            {
                var loadComplete = tagList.Where(x => x.TagName.Contains("LoadComplete")).First();
                loadComplete.Value = false;
                loadComplete.LastRead = DateTime.Now;
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

                    statusTimer.Change(1000, 1000);
                }
            }

        }

        public void LoadTags(List<TagModel> oasTags)
        {
            tagList = oasTags;
        }

        public bool Connect(string ipAddress, string armAddress)
        {
            mlIpAddress = ipAddress;
            mlArmAddress = armAddress;
            if (!(m_connected))
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(mlIpAddress), 7734);

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


        public bool Disconnect()
        {
            try
            {
                if (!(m_connected))
                    return m_connected;

                SendCommand("DA");
                sock.Close();
                m_connected = false;
            }
            catch (Exception e)
            {
                m_connected = false;
            }
            return m_connected;
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

        public string SendFirstPrompt(string promptValue, string promptLength)
        {
            var result = SendCommand("WP 000 " + promptValue + "&" + promptLength);
            return result;
        }
        
        public string SendNextPrompt(string promptValue, string promptLength)
        {
            var result = SendCommand("WA 000 " + promptValue + "&" + promptLength);
            return result;
        }

        private void DynamicDisplayTimer(object State)
        {
            var monitorLoad = SendCommand("RS");
            if (monitorLoad.Contains("BD") && monitorLoad.Contains("TD"))
            {
                loadStatus = 3;
                dynamicDisplayTimer.Change(Timeout.Infinite, Timeout.Infinite);
                statusTimer.Change(1000, 1000);
            }
            else if (monitorLoad.Contains("TP"))
            {
                var grossResult = SendCommand("DY TR02");
                if (grossResult != null)
                {
                    var grossDynamicTag = tagList.Where(x => x.TagName.Contains("DDGrossVolume")).FirstOrDefault();
                    grossDynamicTag.Value = grossResult.Substring(16, 17);
                    grossDynamicTag.LastRead = DateTime.Now;
                }
                var netResult = SendCommand("DY TR03");
                if (netResult != null)
                {
                    var netDynamicTag = tagList.Where(x => x.TagName.Contains("DDNetVolume")).FirstOrDefault();
                    netDynamicTag.Value = netResult.Substring(16, 17);
                    netDynamicTag.LastRead = DateTime.Now;
                }
            }
        }

        private void StatusTimerRoutine(object State)
        {
            if (!m_inStatusTimer)
            {

                m_inStatusTimer = true;
                if (loadStatus == 0)
                {
                    if (promptStep == 1)
                    {
                        updateStatus("Idle");
                        var result = SendCommand("RS");
                        if (result.Contains("KY"))
                        {
                            //resetTagModel();

                            updateStatus("In Prompts");
                            var promptResult = SendCommand("RK");
                            if (promptResult != null)
                            {
                                var tagName = getPromptTag(promptStep).TagName;
                                var promptLength = Convert.ToInt32(getPromptTag(promptStep).PromptLength);
                                var updTag = getTag(tagName);
                                updTag.LastRead = DateTime.Now;
                                updTag.Value = promptResult.Substring(7, promptLength);
                                if(totalPromptCount > 1)
                                {
                                    var nextPrompt = getPromptTag(promptStep + 1);
                                    var nextPromptResult = SendNextPrompt(nextPrompt.Prompt, nextPrompt.PromptLength);
                                    if (nextPromptResult.Contains("OK"))
                                    {
                                        promptStep++;
                                        var nextPromptTag = getTag(nextPrompt.TagName);
                                        nextPromptTag.LastRead = DateTime.Now;
                                        nextPromptTag.Value = 0;
                                    }
                                }

                            }
                        }
                        m_inStatusTimer = false;
                        return;
                    }
                    else if (promptStep <= totalPromptCount)
                    {
                        
                        var result = SendCommand("RS");
                        if (result != null && result.Contains("KY"))
                        {
                            var promptResult = SendCommand("RK");
                            if (promptResult != null)
                            {
                                var tagName = getPromptTag(promptStep).TagName;
                                var promptLength = Convert.ToInt32(getPromptTag(promptStep).PromptLength);
                                var updTag = getTag(tagName);
                                updTag.LastRead = DateTime.Now;
                                updTag.Value = promptResult.Substring(7, promptLength);
                                if(promptStep < totalPromptCount)
                                {
                                    var nextPrompt = getPromptTag(promptStep + 1);
                                    var nextPromptResult = SendNextPrompt(nextPrompt.Prompt, nextPrompt.PromptLength);
                                    if (nextPromptResult.Contains("OK"))
                                    {
                                        promptStep++;
                                        var nextPromptTag = getTag(nextPrompt.TagName);
                                        nextPromptTag.LastRead = DateTime.Now;
                                        nextPromptTag.Value = 0;
                                    }
                                }
                                else
                                {
                                    loadStatus = 1;
                                    promptStep = 1;
                                }
                            }
                        }
                        m_inStatusTimer = false;
                        return;
                    }
                    m_inStatusTimer = false;
                    return;
                }
                else if (loadStatus == 1)//authorize...
                {
                    var authorizeResult = SendCommand("SB 1500");
                    if (authorizeResult.Contains("OK"))
                    {
                        updateStatus("Authorized");
                        var startDateTag = tagList.Where(x => x.TagName.Contains("StartTime")).Select(x => x.TagName).First();
                        var startDate = getTag(startDateTag);
                        startDate.Value = DateTime.Now.ToString();
                        startDate.LastRead = DateTime.Now;
                        var tNumTag = tagList.Where(x => x.TagName.Contains("TransactionNumber")).First();
                        var currentTransaction = SendCommand("TS");
                        if (currentTransaction != null)
                        {
                            tNumTag.Value = Convert.ToInt32(currentTransaction.Substring(7, 10)) + 1;
                            tNumTag.LastRead = DateTime.Now;
                        }

                        
                        loadStatus = 2;
                    }
                    m_inStatusTimer = false;
                    return;
                }
                else if (loadStatus == 2)
                {
                    updateStatus("Transaction In Progress");
                    dynamicDisplayTimer.Change(500, 500);
                    statusTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    m_inStatusTimer = false;
                    return;
                }
                else if (loadStatus == 3)
                {
                    updateStatus("Saving...");

                    var monitorLoad = SendCommand("RS");
                    if(monitorLoad.Contains("BD") && monitorLoad.Contains("TD"))
                    {
                        updateStatus("Complete");
                        var transactionNumber = SendCommand("TS");
                        if (transactionNumber != null)
                        {
                            var updTrans = tagList.Where(x => x.DataPoint != "Prompt");

                            var transNum = transactionNumber.Substring(7, 10);

                            var transData = SendCommand("TR " + transNum);
                            if (transData != null)
                            {
                                DateTime endTime = DateTime.Now;
                                List<string> transactionValues = transData.Split(',').ToList<string>();
                                foreach (var t in updTrans)
                                {
                                    if (t.TagName.Contains("IVVolume"))
                                    {
                                        t.Value = transactionValues[14];
                                        t.LastRead = endTime;
                                    }
                                    else if (t.TagName.Contains("TransactionNumber"))
                                    {
                                        t.Value = transactionValues[1];
                                        t.LastRead = endTime;
                                    }
                                    else if (t.TagName.Contains("IVTotalizer"))
                                    {
                                        t.Value = transactionValues[29];
                                        t.LastRead = endTime;
                                    }
                                    else if (t.TagName.Contains("GVVolume"))
                                    {
                                        t.Value = transactionValues[15];
                                        t.LastRead = endTime;
                                    }
                                    else if (t.TagName.Contains("GVTotalizer"))
                                    {
                                        t.Value = transactionValues[30];
                                        t.LastRead = endTime;
                                    }
                                    else if (t.TagName.Contains("GSTVolume"))
                                    {
                                        t.Value = transactionValues[16];
                                        t.LastRead = endTime;
                                    }
                                    else if (t.TagName.Contains("GSTTotalizer"))
                                    {
                                        t.Value = transactionValues[31];
                                        t.LastRead = endTime;
                                    }
                                    else if (t.TagName.Contains("GSVVolume"))
                                    {
                                        t.Value = transactionValues[17];
                                        t.LastRead = endTime;
                                    }
                                    else if (t.TagName.Contains("GSVTotalizer"))
                                    {
                                        t.Value = transactionValues[32];
                                        t.LastRead = endTime;
                                    }
                                    else if (t.TagName.Contains("EndTime"))
                                    {
                                        t.Value = endTime.ToString();
                                        t.LastRead = endTime;
                                    }
                                    else if (t.TagName.Contains("MeterFactor"))
                                    {
                                        t.Value = transactionValues[23];
                                        t.LastRead = endTime;
                                    }
                                    else if (t.TagName.Contains("Temperature"))
                                    {
                                        t.Value = transactionValues[24];
                                        t.LastRead = endTime;
                                    }
                                    else if (t.TagName.Contains("Density"))
                                    {
                                        t.Value = transactionValues[25];
                                        t.LastRead = endTime;
                                    }
                                    else if (t.TagName.Contains("Pressure"))
                                    {
                                        t.Value = transactionValues[26];
                                        t.LastRead = endTime;
                                    }
                                    else if(t.TagName.Contains("CTL"))
                                    {
                                        t.Value = transactionValues[27];
                                        t.LastRead = endTime;
                                    }
                                    else if(t.TagName.Contains("CPL"))
                                    {
                                        t.Value = transactionValues[28];
                                        t.LastRead = endTime;
                                    }
                                    //else if (t.TagName.Contains("LoadComplete"))
                                    //{
                                    //    t.Value = true;
                                    //    t.LastRead = endTime;
                                    //}
                                }
                                loadStatus = 4;
                            }
                        }
                    }
                    m_inStatusTimer = false;
                    return;
                }
                else if (loadStatus == 4)
                {
                    var setComplete = tagList.Where(x => x.TagName.Contains("LoadComplete")).First();
                    setComplete.Value = true;
                    setComplete.LastRead = DateTime.Now;
                    Thread.Sleep(5000);

                    resetTagModel();
                    LoadPrompts(prompts);
                    m_inStatusTimer = false;
                    return;
                }

                //statusTimer.Change(1000, 1000);
                m_inStatusTimer = false;
            }
        }

        private Prompts getPromptTag(int promptStep)
        {
            if (promptStep > totalPromptCount)
                return null;

            var promptTag = prompts.Where(x => x.Order == promptStep).First();
            return promptTag;
        }

        private TagModel getTag(string tagName)
        {
            var list = tagList.Where(x => x.TagName == tagName).First();

            return list;
        }

        private void resetTagModel()
        {
            var resetTags = tagList;
            foreach(var t in tagList)
            {
                t.LastRead = DateTime.Now;
                t.Value = 0;
            }
        }


        private void updateStatus(string status)
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

        Socket socket()
        {
            return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
    }
}

