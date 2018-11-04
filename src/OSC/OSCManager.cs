using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityOSC;
using Newtonsoft.Json;
using System.Net;

public class OSCManager : MonoBehaviour {
    [SerializeField]
    private bool isSelfInit;
    public bool HasInit{
        get;
        private set;
    }

    public enum InitMode
    {
        SettingsFile,
        Inspector
    }
    public InitMode initMode;
    [SerializeField]
    string settingsFileName;
    [SerializeField]
    OSCHandler.Settings settings;
    Queue queue;
    //public event PacketReceivedEventHandler PacketReceivedEvent;
    public delegate void OSCMessageReceiveEventHandler(OSCMessage message);
    public event OSCMessageReceiveEventHandler OSCMessageReceivedEvent;

    // Use this for initialization
    void Awake () {
        if (!isSelfInit) return;

        switch (initMode)
        {
            case InitMode.SettingsFile:
                this.settings = loadSettings();
                break;
                
            case InitMode.Inspector:

                break;
        }

        if (settings == null) return;
        this.Init(settings);
    }

    public void Init(OSCHandler.Settings settings)
    {
        this.Init(settings.ServerList, settings.ClientList);
    }

    public void Init(List<OSCHandler.ServerInfo> serverInfoList, List<OSCHandler.ClientInfo> clientInfoList)
    {
        // OSCHandler.Instance.Close();
        queue = new Queue();
        queue = Queue.Synchronized(queue);
        OSCHandler.Instance.PacketReceivedEvent += Instance_PacketReceivedEvent;
        OSCHandler.Instance.ErrorEvent += Instance_ErrorEvent;
        OSCHandler.Instance.Init(serverInfoList, clientInfoList);
        HasInit = true;
    }

    private void Instance_PacketReceivedEvent(OSCServer sender, OSCPacket packet)
    {
        queue.Enqueue(packet);
    }

    private void Instance_ErrorEvent(string message)
    {
        throw new System.NotImplementedException();
    }

    public void CreateClient(OSCHandler.ClientInfo clientInfo)
    {
        IPAddress ipAddress;
        if(!IPAddress.TryParse(clientInfo.IPAddress, out ipAddress))return;
        this.CreateClient(clientInfo.Name, ipAddress, clientInfo.Port);
    }

    public void CreateClient(string clientId, IPAddress ipAddress, int port)
    {
        OSCHandler.Instance.CreateClient(clientId, ipAddress, port);
    }

    public void CreateServer(OSCHandler.ServerInfo serverInfo)
    {
        OSCHandler.Instance.CreateServer(serverInfo.Name, serverInfo.Port);
        HasInit = true;
    }

    public void CreateServer(string serverId, int port)
    {
        OSCHandler.Instance.CreateServer(serverId, port);
        HasInit = true;
    }

    OSCHandler.Settings loadSettings()
    {
        OSCHandler.Settings settings = null;
        if (string.IsNullOrEmpty(settingsFileName)) return settings;
        var path = Path.Combine(Application.streamingAssetsPath, settingsFileName);
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            settings = JsonConvert.DeserializeObject<OSCHandler.Settings>(json);
        }
        return settings;
    }
    // Update is called once per frame
    void Update()
    {
        if(!HasInit)return;
        while (queue.Count > 0)
        {
            OSCPacket packet = queue.Dequeue() as OSCPacket;
            if (packet.IsBundle())
            {
                // OSCBundleの場合
                OSCBundle bundle = packet as OSCBundle;
                foreach (OSCMessage msg in bundle.Data)
                {
                    if (OSCMessageReceivedEvent != null) OSCMessageReceivedEvent(msg);
                }
            }
            else
            {
                OSCMessage msg = packet as OSCMessage;
                if (OSCMessageReceivedEvent != null) OSCMessageReceivedEvent(msg);
            }
        }
    }

    private void OnDestroy()
    {
        OSCHandler.Instance.Close();
        HasInit = false;
    }

    [ContextMenu("ExportSettings")]
    void exportSettings()
    {
        if (string.IsNullOrEmpty(settingsFileName)) return;
        var path = Path.Combine(Application.streamingAssetsPath, settingsFileName);
        var json = JsonConvert.SerializeObject(settings);
        File.WriteAllText(path, json);
    }
}
