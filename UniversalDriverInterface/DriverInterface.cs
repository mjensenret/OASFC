using OASDriverInterface;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using IntegrationDrivers.Models;
using static IntegrationDrivers.MicroLoadUtilities;
using System.Linq;
using static IntegrationDrivers.Infrastructure.Enumerations;

namespace UniversalDriverInterface
{
    public class DriverInterface
    {

        //Demo Enumerations
        private bool InstanceFieldsInitialized = false;

        private string m_IpAddress = "10.4.12.26";
        private string m_Port = "7734";
        private string m_VolumeUnits = "Gallons";
        private string m_armAddress = "01";
        private int m_cartId = 1;

        private int promptStep = 0;
        private int promptCount;
        private string initialPrompt;
        private List<Prompts> prompts = new List<Prompts>();
        private List<TagModel> tagList = new List<TagModel>();
        public Queue m_DataValuesQueue = new Queue();
        private Hashtable m_DataValuesHashTable = new Hashtable();

        private void InitializeInstanceFields()
        {
            //promptStep = 1;
            
            oasTagTimer = new Timer(UpdateOASTagTimerRoutine, null, Timeout.Infinite, Timeout.Infinite);
            readMicroLoad = new Timer(ReadMicroloadValuesRoutine, null, Timeout.Infinite, Timeout.Infinite);
        }

        private IntegrationDrivers.BaseDriver m_device;
        //Interface to OAS - Do not remove or modify
        private OASDriverInterface.OASDriverInterface m_OASDriverInterface;

        // Require Variables
        private string m_DriverName = "Fuel Cart Interface";

        //Lists of  Properties For Driver Interface and Tags
        private List<ClassProperty> m_DriverProps = new List<ClassProperty>();

        //Demo Code
        private bool m_Connected;
        //Active Tags
        private Hashtable m_Tags = new Hashtable();
        private Hashtable m_StaticTagValues = new Hashtable();
        //Used to simulate different Polling Rates
        private Hashtable m_LastUpdateTime = new Hashtable();
        private Timer oasTagTimer;
        private bool m_InTimerRoutine;
        private Timer readMicroLoad;

        //Machine Name Must be unique for each instance of the driver running on local and remote devices.
        //In the working example this is read from appsettings.json.  You can dynamically set this as well.  This must match the Machine Name defined in the Driver Interface of the OAS Service.
        private string m_MachineName = "FuelingInterface";

        public DriverInterface(string OASServiceNode, string LiveDataCloudNode, int PortNumber, string MachineName, bool StoreAndForward, string UserName, string Password, string DeviceType)
        {
            if (!InstanceFieldsInitialized)
            {
                InitializeInstanceFields();
                InstanceFieldsInitialized = true;
                
            }
            m_MachineName = MachineName;
            m_OASDriverInterface = new OASDriverInterface.OASDriverInterface(m_DriverName, OASServiceNode, LiveDataCloudNode, PortNumber, MachineName, StoreAndForward, UserName, Password);
            if(DeviceType == "MicroLoad")
            {
                Console.WriteLine("Loading Microload....");
                m_device = new IntegrationDrivers.MicroLoadUtilities();
            }
            else if (DeviceType == "AccuLoad")
            {
                Console.WriteLine("Loading AccuLoad....");
                m_device = new IntegrationDrivers.AccuLoadUtilities();
            }
            

            m_OASDriverInterface.SetDefaults(GetDefaultDriverConfig(), false);
            m_OASDriverInterface.SetDefaults(GetDefaultTagConfig(), true);
            m_OASDriverInterface.AddDriverInterfaceToService(GetDriverInterfaceToAdd()); // Optional, automatically add / update driver interface.
            m_OASDriverInterface.AddTagsToService(GetTagsToAdd()); // Optional, automatically add / update tags.
            SubscribeToEvents();
        }

        #region Driver Section

        public string DriverName
        {
            get
            {
                return m_DriverName;
            }
            set
            {
                m_DriverName = value;
            }
        }

        public List<ClassProperty> DriverConfig
        {
            get
            {
                return m_DriverProps;
            }
            set
            {
                m_DriverProps = value;
            }
        }

        // Create driver and field definitions
        public List<ClassProperty> GetDefaultDriverConfig()
        {
            Console.WriteLine("GetDefaultDriverConfig");

            try
            {
                List<ClassProperty> DriverProps = new List<ClassProperty>();
                DriverProps.Clear();
                //DriverProps.Add(new ClassProperty("APIKey", "API Key", "The OpenWeatherMap API Key used for tracking requests. \nGet your own custom API Key by registering at openweathermap.org", typeof(string), m_APIKey, ClassProperty.ePropInputType.Manual));
                //DriverProps.Add(new ClassProperty("DataRefreshTime", "Data Refresh Time", "The maximum number of seconds the data will be updated on the OpenWeatherMap server. \nThis is determined by the purchased Subscription and defaults to 2 hours for FREE subscriptions.\nThis acts as the global minimum time to wait before hitting the API server for updates despite what is set per Tag.", typeof(int), m_DataRefreshTime, ClassProperty.ePropInputType.Manual));
                DriverProps.Add(new ClassProperty("IPAddress", "IP Address", "Device IP Address", typeof(string), m_IpAddress, ClassProperty.ePropInputType.Manual));
                DriverProps.Add(new ClassProperty("ArmAddress", "Arm Address", "Address of the arm to connect to (number must include the preceding 0).", typeof(string), m_armAddress, ClassProperty.ePropInputType.Manual));
                DriverProps.Add(new ClassProperty("Port", "Port", "TCP Port to connect to.", typeof(string), m_Port, ClassProperty.ePropInputType.Manual));
                //DriverProps.Add(new ClassProperty("Units", "Units", "Used to set the units the Accuload is configured to measure", typeof(BaseDriver.VolumeUnits), BaseDriver.VolumeUnits.Gallons, ClassProperty.ePropInputType.Manual));
                //DriverProps.Add(new ClassProperty("Test", "Test descr", "", typeof(string), "", ClassProperty.ePropInputType.Manual));
                return DriverProps;
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} : {1}", "GetDefaultDriverConfig", ex.Message);
                m_OASDriverInterface.UpdateSystemError(true, "Configuration", 1, "GetDefaultDriverConfig Exception: " + ex.Message);
                return null;
            }
        }

        // Optionally add driver interface automatically when connected
        public List<ClassProperty> GetDriverInterfaceToAdd()
        {
            Console.WriteLine("GetDriverInterfaceToAdd");
            try
            {
                List<ClassProperty> DriverProps = new List<ClassProperty>();
                // The name of the Driver Interface needs to be unique for each driver instance deployed
                DriverProps.Add(new ClassProperty("Name", "", "", typeof(string), m_DriverName + "-" + m_MachineName, ClassProperty.ePropInputType.Manual));
                DriverProps.Add(new ClassProperty("IPAddress", "IP Address", "Device IP Address", typeof(string), m_IpAddress, ClassProperty.ePropInputType.Manual));
                DriverProps.Add(new ClassProperty("ArmAddress", "Arm Address", "Address of the arm to connect to (number must include the preceding 0).", typeof(string), m_armAddress, ClassProperty.ePropInputType.Manual));
                //DriverProps.Add(new ClassProperty("Units", "Units", "Used to set the units the Accuload is configured to measure", typeof(BaseDriver.VolumeUnits), BaseDriver.VolumeUnits.Gallons, ClassProperty.ePropInputType.Manual));
                DriverProps.Add(new ClassProperty("Port", "Port", "TCP Port to connect to.", typeof(string), m_Port, ClassProperty.ePropInputType.Manual));
                return DriverProps;
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} : {1}", "GetDriverInterfaceToAdd", ex.Message);
                m_OASDriverInterface.UpdateSystemError(true, "Configuration", 2, "GetDriverInterfaceToAdd Exception: " + ex.Message);
                return null;
            }
        }

        public void Connect()
        {
            Console.WriteLine("Connect");
            try
            {
                m_IpAddress = Convert.ToString(BaseFunctions.GetPropValue(m_DriverProps, "IPAddress"));
                m_armAddress = Convert.ToString(BaseFunctions.GetPropValue(m_DriverProps, "ArmAddress"));
                m_Port = Convert.ToString(BaseFunctions.GetPropValue(m_DriverProps, "Port"));
                //m_VolumeUnits = Convert.ToString(BaseFunctions.GetPropValue(m_DriverProps, "Units"));
                
                
                if (!(m_Connected))
                {
                    m_Connected = m_device.Connect(m_IpAddress, m_armAddress, m_Port);
                }
                m_Connected = true;

                oasTagTimer.Change(500, 500);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} : {1}", "Connect", ex.Message);
                m_OASDriverInterface.UpdateSystemError(true, "Connect", 1, "GetDefaultDriverConfig Exception: " + ex.Message);
            }
        }

        public bool Disconnect()
        {
            Console.WriteLine("Disconnect");
            try
            {
                if (!(m_Connected))
                    return m_Connected;

                m_device.Disconnect();

                lock (m_Tags.SyncRoot)
                    m_Tags.Clear();

                oasTagTimer.Change(Timeout.Infinite, Timeout.Infinite);

                m_Connected = false;
                return m_Connected;
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} : {1}", "Disconnect", ex.Message);
                m_OASDriverInterface.UpdateSystemError(true, "Disconnect", 1, "GetDefaultDriverConfig Exception: " + ex.Message);
                return false;
            }
        }
        #endregion

        #region Tag Section
        // This Function defines the tag configuration properties and builds the UI For the Tag Configuration Properties
        // Place items in the order you want them to appear in the UI
        // Adding "-->" to the Property Description will add the next property to the right of the current property.
        // If you have a blank String for the Property Help no Help button will be displayed.

        // Note: Do not use the property names TagName and PollingRate.  These are OAS property names that are already defined for the interface.
        public List<ClassProperty> GetDefaultTagConfig()
        {
            //Console.WriteLine("GetDefaultTagConfig");
            try
            {
                List<ClassProperty> m_TagProps = new List<ClassProperty>();

                m_TagProps.Add(new ClassProperty("DataPoint", "Data Point", "Choose the data point that this tag is recording.", typeof(DataPoint), DataPoint.Status, ClassProperty.ePropInputType.Manual));
                m_TagProps.Add(new ClassProperty("PromptText", "Prompt Text", "Enter the text that you would like to display on the MicroLoad (21 character limit)", typeof(string), "Enter Prompt", ClassProperty.ePropInputType.Manual, "Visible,DataPoint.Prompt"));
                m_TagProps.Add(new ClassProperty("PromptOrder", "Prompt Order", "Order of the prompt", typeof(int), 1, ClassProperty.ePropInputType.Manual, "Visible,DataPoint.Prompt"));
                m_TagProps.Add(new ClassProperty("PromptLength", "Prompt Length", "How many characters to allow the operator to enter", typeof(string), "05", ClassProperty.ePropInputType.Manual, "Visible,DataPoint.Prompt"));
                m_TagProps.Add(new ClassProperty("MeasurementType", "Measurement Type", "Select which volume type this tag is used for (IV, Gross, GST, GSV, or Mass", typeof(MeasurementType), MeasurementType.IV, ClassProperty.ePropInputType.Manual, "Visible,DataPoint.Volume|DataPoint.Totalizer"));

                return m_TagProps;
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} : {1}", "GetDefaultTagConfig", ex.Message);
                m_OASDriverInterface.UpdateSystemError(true, "Configuration", 3, "GetDefaultTagConfig Exception: " + ex.Message);
                return null;
            }
        }

        // Optionally add tags automatically
        public List<List<ClassProperty>> GetTagsToAdd()
        {
            //Console.WriteLine("GetTagsToAdd");
            try
            {
                List<List<ClassProperty>> m_Tags = new List<List<ClassProperty>>();

                string[] TagsToAdd = new[] {
                    "CartId",
                    "Status",
                    "OperatorId",
                    "OrderId",
                    "SequenceId",
                    "StartTime",
                    "EndTime",
                    "TransactionNumber",
                    "IVVolume",
                    "IVTotalizer",
                    "GVVolume",
                    "GVTotalizer",
                    "GSTVolume",
                    "GSTTotalizer",
                    "GSVVolume",
                    "GSVTotalizer",
                    "MeterFactor",
                    "Temperature",
                    "Density",
                    "Pressure",
                    "CTL",
                    "CPL",
                    "LoadComplete",
                    "DDGrossVolume",
                    "DDNetVolume"
                };

                foreach (string tagName in TagsToAdd)
                {
                    List<ClassProperty> m_TagProps = new List<ClassProperty>();
                    string dType;
                    string promptText = "";
                    int promptOrder = 0;
                    string dataPoint;
                    string type = "";
                    switch (tagName)
                    {
                        case "OperatorId":
                            {
                                dType = "Integer";
                                promptOrder = 1;
                                promptText = "Operator Id";
                                dataPoint = "Prompt";
                                break;
                            }
                        case "OrderId":
                            {
                                dType = "Integer";
                                promptOrder = 2;
                                promptText = "Order Id";
                                dataPoint = "Prompt";
                                break;
                            }
                        case "SequenceId":
                            {
                                dType = "Integer";
                                promptOrder = 3;
                                promptText = "Sequence Id";
                                dataPoint = "Prompt";
                                break;
                            }
                        case "Status":
                            {
                                dType = "String";
                                dataPoint = "Status";
                                break;
                            }
                        case "StartTime":
                            {
                                dType = "String";
                                dataPoint = "Time";
                                break;
                            }
                        case "EndTime":
                            {
                                dType = "String";
                                dataPoint = "Time";
                                break;
                            }
                        case "TransactionNumber":
                            {
                                dType = "Integer";
                                dataPoint = "TransactionNum";
                                break;
                            }
                        case "CartId":
                            {
                                dType = "Integer";
                                dataPoint = "Configuration";
                                break;
                            }
                        case "IVVolume" :
                            {
                                dType = "Double";
                                dataPoint = "Volume";
                                type = "IV";
                                break;
                            }
                        case "IVTotalizer":
                            {
                                dType = "Double";
                                dataPoint = "Totalizer";
                                type = "IV";
                                break;
                            }
                        case "GVVolume":
                            {
                                dType = "Double";
                                dataPoint = "Volume";
                                type = "Gross";
                                break;
                            }
                        case "GVTotalizer":
                            {
                                dType = "Double";
                                dataPoint = "Totalizer";
                                type = "Gross";
                                break;
                            }
                        case "GSTVolume":
                            {
                                dType = "Double";
                                dataPoint = "Volume";
                                type = "GST";
                                break;
                            }
                        case "GSTTotalizer":
                            {
                                dType = "Double";
                                dataPoint = "Totalizer";
                                type = "GST";
                                break;
                            }
                        case "GSVVolume":
                            {
                                dType = "Double";
                                dataPoint = "Volume";
                                type = "GSV";
                                break;
                            }
                        case "GSVTotalizer":
                            {
                                dType = "Double";
                                dataPoint = "Totalizer";
                                type = "GSV";
                                break;
                            }
                        case "DDGrossVolume":
                            {
                                dType = "Double";
                                dataPoint = "Volume";
                                type = "Gross";
                                break;
                            }
                        case "DDNetVolume":
                            {
                                dType = "Double";
                                dataPoint = "Volume";
                                type = "GST";
                                break;
                            }
                        case "MeterFactor":
                        case "Temperature":
                        case "Density":
                        case "Pressure":
                        case "CTL":
                        case "CPL":
                            {
                                dType = "Double";
                                dataPoint = "TransactionAvgs";
                                break;
                            }
                        case "LoadComplete":
                            {
                                dType = "Boolean";
                                dataPoint = "Status";
                                break;
                            }
                        default:
                            {
                                dType = "String";
                                dataPoint = "Status";
                                promptText = "";
                                break;
                            }
                    }


                    m_TagProps.Add(new ClassProperty("Tag", "", "", typeof(string), string.Format($"{m_MachineName}.{tagName}"), ClassProperty.ePropInputType.Manual));
                    m_TagProps.Add(new ClassProperty("Value - Data Type", "", "", typeof(string), dType, ClassProperty.ePropInputType.Manual));
                    m_TagProps.Add(new ClassProperty("Value - Source", "", "", typeof(string), "UDI " + m_DriverName, ClassProperty.ePropInputType.Manual));
                    m_TagProps.Add(new ClassProperty("Value - Driver Interface", "", "", typeof(string), m_DriverName + "-" + m_MachineName, ClassProperty.ePropInputType.Manual));

                    DataPoint dp = (DataPoint)Enum.Parse(typeof(DataPoint), dataPoint);
                    m_TagProps.Add(new ClassProperty("Value - UDI " + m_DriverName + " DataPoint", "", "", typeof(DataPoint), dp, ClassProperty.ePropInputType.Manual));
                    if (dp == DataPoint.Prompt)
                    {
                        m_TagProps.Add(new ClassProperty("Value - UDI " + m_DriverName + " PromptOrder", "Prompt Order", "Enter the order to display this prompt.  Ensure that two prompts do not have the same order.", typeof(int), promptOrder, ClassProperty.ePropInputType.Manual));
                        m_TagProps.Add(new ClassProperty("Value - UDI " + m_DriverName + " PromptText", "Prompt Text", "Enter text to display on the Microload", typeof(string), promptText, ClassProperty.ePropInputType.Manual));
                        m_TagProps.Add(new ClassProperty("Value - UDI " + m_DriverName + " PromptLength", "Prompt Length", "Enter the amount of characters the operator can enter on the ML (max 21)", typeof(string), "05", ClassProperty.ePropInputType.Manual));
                    }
                    if (dp == DataPoint.Volume || dp == DataPoint.Totalizer)
                    {
                        MeasurementType v = (MeasurementType)Enum.Parse(typeof(MeasurementType), type);
                        m_TagProps.Add(new ClassProperty("Value - UDI " + m_DriverName + " MeasurementType", "Measurement Type", "Choose which type of volume value this tag records (IV, Gross, GST, GSV).", typeof(MeasurementType), v, ClassProperty.ePropInputType.Manual));
                    }

                    m_Tags.Add(m_TagProps);
                }
                return m_Tags;
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} : {1}", "GetTagsToAdd", ex.Message);
                m_OASDriverInterface.UpdateSystemError(true, "Configuration", 4, "GetTagsToAdd Exception: " + ex.Message);
                return null;
            }
        }


        // This method is called from the service to add tags to be monitored continuously.
        public void AddTags(List<ClassProperty>[] Tags)
        {
            Console.WriteLine("AddTags");
            var localTime = DateTime.Now;
            try
            {
                if (prompts.Count > 0)
                    prompts.Clear();

                if (tagList.Count > 0)
                    tagList.Clear();

                // Add Logic. Props is a list of ClassProperty in the same order of Get Tag Config
                lock (m_Tags.SyncRoot)
                {
                    foreach (List<ClassProperty> Props in Tags)
                    {
                        // Use the TagName as a unique identifier for the Tag Name and Parameter being interfaced with.
                        string TagID = (string)BaseFunctions.GetPropValue(Props, "TagName");
                        DataPoint dp = (DataPoint)BaseFunctions.GetPropValue(Props, "DataPoint");
                        if (dp == DataPoint.Prompt)
                        {
                            int order = (int)BaseFunctions.GetPropValue(Props, "PromptOrder");
                            string promptText = (string)BaseFunctions.GetPropValue(Props, "PromptText");
                            string promptLength = (string)BaseFunctions.GetPropValue(Props, "PromptLength");
                            prompts.Add(new Prompts { TagName = TagID, Order = order, Prompt = promptText, PromptLength = promptLength });
                        }

                        tagList.Add(new TagModel
                        {
                            TagName = TagID,
                            LastRead = localTime,
                            PollingRate = (double)BaseFunctions.GetPropValue(Props, "PollingRate"),
                            Value = 0,
                            DataPoint = dp.ToString()
                            //ValueDataType = (string)BaseFunctions.GetPropValue(Props, "VolumeType")
                        });

                        if (m_Tags.Contains(TagID))
                            m_Tags[TagID] = Props;
                        else
                            m_Tags.Add(TagID, Props);
                        if (m_LastUpdateTime.Contains(TagID))
                            m_LastUpdateTime.Remove(TagID);
                    }

                    m_device.LoadTags(tagList);
                    m_device.LoadPrompts(prompts);
                    
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} : {1}", "AddTags", ex.Message);
                m_OASDriverInterface.UpdateSystemError(true, "Communications", 1, "AddTags Exception: " + ex.Message);
            }
        }

        // remove tags from being monitored continuously
        public void RemoveTags(string[] Tags)
        {
            Console.WriteLine("RemoveTags");
            try
            {
                lock (m_Tags.SyncRoot)
                {
                    foreach (string TagID in Tags)
                    {
                        if (m_Tags.Contains(TagID))
                            m_Tags.Remove(TagID);
                        if (m_LastUpdateTime.Contains(TagID))
                            m_LastUpdateTime.Remove(TagID);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} : {1}", "RemoveTags", ex.Message);
                m_OASDriverInterface.UpdateSystemError(true, "Communications", 2, "RemoveTags Exception: " + ex.Message);
            }
        }

        // This call is performed when a Device Read is executed in OAS.
        public ClassTagValue[] SyncRead(List<ClassProperty>[] Tags)
        {
            //Console.WriteLine("SyncRead");
            //try
            //{
            //    DateTime currentTime = DateTime.Now;
            //    DateTime dt = DateTime.MinValue;
            //    double localSeconds = currentTime.Second + (currentTime.Millisecond / (double)1000);

            //    ArrayList localArrayList = new ArrayList();

            //    lock (m_StaticTagValues.SyncRoot)
            //    {
            //        foreach (List<ClassProperty> TagItems in Tags)
            //        {
            //            string TagID = (string)BaseFunctions.GetPropValue(TagItems, "TagName");
            //            string postalCode = (string)BaseFunctions.GetPropValue(TagItems, "PostalCode");
            //            TempScale tempScale = (TempScale)BaseFunctions.GetPropValue(TagItems, "TempScale");

            //            object Value = null;
            //            try
            //            {
            //                Value = WeatherUtils.GenerateTagValue(TagID, TagItems, m_DataRefreshTime, m_APIKey);
            //                dt = (DateTime)WeatherUtils.m_UpdateTimes[postalCode];
            //            }
            //            catch
            //            {
            //            }

            //            bool Quality = false;
            //            if (Value != null && !string.IsNullOrWhiteSpace((string)Value))
            //                Quality = true;
            //            localArrayList.Add(new ClassTagValue(TagID, Value, dt, Quality));
            //        }
            //    }

            //    return (ClassTagValue[])localArrayList.ToArray(typeof(ClassTagValue));
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("{0} : {1}", "SyncRead", ex.Message);
            //    m_OASDriverInterface.UpdateSystemError(true, "Communications", 3, "SyncRead Exception: " + ex.Message);
            //    return null;
            //}
            return null;

        }


        // unused in this example, but would be where a Write call would be made to the remote API based on values being set in a Tag
        public void WriteValues(string[] TagIDs, object[] Values, List<ClassProperty>[] TagProperties)
        {
            Console.WriteLine("WriteValues");
            //try
            //{
            //    // Add write Logic
            //    Int32 Index;
            //    var loopTo = TagIDs.GetLength(0) - 1;
            //    for (Index = 0; Index <= loopTo; Index++)
            //    {
            //        lock (m_StaticTagValues.SyncRoot)
            //        {
            //            if (m_StaticTagValues.Contains(TagIDs[Index]))
            //                m_StaticTagValues[TagIDs[Index]] = Values[Index];
            //            else
            //                m_StaticTagValues.Add(TagIDs[Index], Values[Index]);
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("{0} : {1}", "WriteValues", ex.Message);
            //    m_OASDriverInterface.UpdateSystemError(true, "Communications", 4, "WriteValues Exception: " + ex.Message);
            //}
        }

        #endregion

        #region Demo Driver Code
        //This is a simple example of getting the properties of a tag and using that to generate a update to the tag value
        
        private void ReadMicroloadValuesRoutine(object State)
        {
            tagList = m_device.UpdateModel(tagList);
            foreach(var t in tagList)
            {
                Console.WriteLine(t);
            }
        }


        private void UpdateOASTagTimerRoutine(object State)
        {
            try
            {
                if (m_InTimerRoutine)
                    return;
                m_InTimerRoutine = true;
                DateTime currentTime = DateTime.Now;
                double localSeconds = currentTime.Second + (currentTime.Millisecond / (double)1000);

                tagList = m_device.ReturnCurrentTaglist();

                ArrayList localArrayList = new ArrayList();

                lock (m_Tags.SyncRoot)
                {
                    lock (m_StaticTagValues.SyncRoot)
                    {
                        foreach (DictionaryEntry de in m_Tags)
                        {
                            string TagID = (string)de.Key;
                            List<ClassProperty> TagItems = (List<ClassProperty>)de.Value;

                            // Just simulating using the PollingRate property
                            bool OKToPoll = true;
                            if (m_LastUpdateTime.Contains(TagID))
                            {
                                double PollingRate = (double)BaseFunctions.GetPropValue(TagItems, "PollingRate");
                                DateTime lastUpdateTime = (DateTime)m_LastUpdateTime[TagID];
                                if (lastUpdateTime.AddSeconds(PollingRate) > currentTime)
                                    OKToPoll = false;
                            }

                            if (OKToPoll)
                            {
                                if (m_LastUpdateTime.Contains(TagID))
                                    m_LastUpdateTime[TagID] = currentTime;
                                else
                                    m_LastUpdateTime.Add(TagID, currentTime);

                                object Value = null;

                                Value = tagList.Where(x => x.TagName == TagID).First().Value;
                               
                                bool Quality = false;
                                if (Value != null && !string.IsNullOrWhiteSpace(Value.ToString()))
                                    Quality = true;

                                // You can include mutiple values to the same tag with different timestamps in the same callback if you like.
                                // In this example it just updates when the timer fires and the check for the PollingRate succeeds.
                                localArrayList.Add(new ClassTagValue(TagID, Value, currentTime, Quality));
                            }
                        }
                    }
                }

                if (localArrayList.Count > 0)
                    // Send values to OAS Service
                    m_OASDriverInterface.AsyncReadCallback((ClassTagValue[])localArrayList.ToArray(typeof(ClassTagValue)));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} : {1}", "TimerRoutine", ex.Message);
                m_OASDriverInterface.UpdateSystemError(true, "Communications", 5, "TimerRoutine Exception: " + ex.Message);
            }
            m_InTimerRoutine = false;
        }

        //Required events that call local functions. - Do not remove or modify.
        //**********************************************************************
        private void m_OASDriverInterface_AddTags(List<ClassProperty>[] TagsToAdd)
        {
            AddTags(TagsToAdd);
        }

        private void m_OASDriverInterface_ConnectState(bool NewConnectState)
        {
            if (NewConnectState)
            {
                Connect();
            }
            else
            {
                Disconnect();
            }
        }

        private void m_OASDriverInterface_DriverConfig(List<ClassProperty> NewDriverConfig)
        {
            DriverConfig = NewDriverConfig;
        }

        private void m_OASDriverInterface_RemoveTags(string[] TagsToRemove)
        {
            RemoveTags(TagsToRemove);
        }

        private void m_OASDriverInterface_SyncRead(List<ClassProperty>[] TagsToRead)
        {
            m_OASDriverInterface.AsyncReadCallback(SyncRead(TagsToRead));
        }

        private void m_OASDriverInterface_WriteValues(string[] TagIDs, object[] Values, List<ClassProperty>[] TagProperties)
        {
            WriteValues(TagIDs, Values, TagProperties);
        }
        //**********************************************************************

        #endregion

        private bool EventsSubscribed = false;
        private void SubscribeToEvents()
        {
            if (EventsSubscribed)
                return;
            else
                EventsSubscribed = true;

            m_OASDriverInterface.AddTags += m_OASDriverInterface_AddTags;
            m_OASDriverInterface.ConnectState += m_OASDriverInterface_ConnectState;
            m_OASDriverInterface.DriverConfig += m_OASDriverInterface_DriverConfig;
            m_OASDriverInterface.RemoveTags += m_OASDriverInterface_RemoveTags;
            m_OASDriverInterface.SyncRead += m_OASDriverInterface_SyncRead;
            m_OASDriverInterface.WriteValues += m_OASDriverInterface_WriteValues;
        }

    }
}
