using IntegrationDrivers.Models;
using OASDriverInterface;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace IntegrationDrivers
{
    public class AccuLoadUtilities : BaseDriver
    {
        private int m_CartId = 1;
        private bool m_inStatusTimer = false;
        private Timer statusTimer;
        private Timer dynamicDisplayTimer;
        private bool m_connected;

        public AccuLoadUtilities() :
            base()
        {
            statusTimer = new Timer(StatusTimerRoutine, null, Timeout.Infinite, Timeout.Infinite);
            dynamicDisplayTimer = new Timer(DynamicDisplayTimer, null, Timeout.Infinite, Timeout.Infinite);
        }

        public override bool Connect(string ipAddress, string armAddress, string port)
        {
            m_connected = base.Connect(ipAddress, armAddress, port);
            if(m_connected)
            {
                statusTimer.Change(1000, 1000);
            }
            return m_connected;
        }

        public override bool Disconnect()
        {
            SendCommand("DA");
            return base.Disconnect();
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

                var cartId = tagList.Where(x => x.TagName.Contains("CartId")).First();
                cartId.Value = m_CartId;
                cartId.LastRead = DateTime.Now;

                if (loadStatus == 0)
                {
                    if (promptStep == 1)
                    {
                        updateStatus("Idle");
                        var result = SendCommand("RS");
                        if (result.Contains("KY"))
                        {
                            resetTagModel();

                            updateStatus("In Prompts");
                            var promptResult = SendCommand("RK");
                            if (promptResult != null)
                            {
                                var tagName = getPromptTag(promptStep).TagName;
                                var promptLength = Convert.ToInt32(getPromptTag(promptStep).PromptLength);
                                var updTag = getTag(tagName);
                                updTag.LastRead = DateTime.Now;
                                updTag.Value = promptResult.Substring(7, promptLength);
                                if (totalPromptCount > 1)
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
                                if (promptStep < totalPromptCount)
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
                    if (monitorLoad.Contains("BD") && monitorLoad.Contains("TD"))
                    {

                        var transactionNumber = SendCommand("TS");
                        if (transactionNumber != null)
                        {
                            Stopwatch sw = new Stopwatch();

                            sw.Start();
                            var updTrans = tagList.Where(x => x.DataPoint != "Prompt");

                            var transNum = transactionNumber.Substring(7, 10);
                            sw.Stop();
                            Console.WriteLine($"updTrans execution time: {sw.ElapsedMilliseconds}");
                            sw.Reset();
                            sw.Start();
                            var transData = SendCommand("TR " + transNum);
                            sw.Stop();
                            Console.WriteLine($"Get TransData time: {sw.ElapsedMilliseconds}");
                            if (transData != null)
                            {
                                sw.Reset();
                                sw.Start();
                                DateTime endTime = DateTime.Now;
                                List<string> transactionValues = transData.Split(',').ToList<string>();
                                sw.Stop();
                                Console.WriteLine($"List execution time: {sw.ElapsedMilliseconds}");
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
                                    else if (t.TagName.Contains("CTL"))
                                    {
                                        t.Value = transactionValues[27];
                                        t.LastRead = endTime;
                                    }
                                    else if (t.TagName.Contains("CPL"))
                                    {
                                        t.Value = transactionValues[28];
                                        t.LastRead = endTime;
                                    }
                                }

                                Thread.Sleep(5000);

                                loadStatus = 4;
                            }
                        }
                    }
                    m_inStatusTimer = false;
                    return;
                }
                else if (loadStatus == 4)
                {
                    updateStatus("Complete");

                    var setComplete = tagList.Where(x => x.TagName.Contains("LoadComplete")).First();
                    setComplete.Value = true;
                    setComplete.LastRead = DateTime.Now;

                    LoadPrompts(prompts);
                    m_inStatusTimer = false;
                    return;
                }

                statusTimer.Change(1000, 1000);
                m_inStatusTimer = false;
            }
        }
    }
}

