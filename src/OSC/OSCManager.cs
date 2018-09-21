using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityOSC;
using Newtonsoft.Json;

public class OSCManager : MonoBehaviour {
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
        queue = new Queue();
        queue = Queue.Synchronized(queue);
        OSCHandler.Instance.PacketReceivedEvent += Instance_PacketReceivedEvent;
        OSCHandler.Instance.ErrorEvent += Instance_ErrorEvent;
        switch (initMode)
        {
            case InitMode.SettingsFile:
                this.settings = loadSettings();
                break;
                
            case InitMode.Inspector:

                break;
        }

        if (settings == null) return;
        OSCHandler.Instance.Init(settings);
    }

    private void Instance_PacketReceivedEvent(OSCServer sender, OSCPacket packet)
    {
        queue.Enqueue(packet);
    }

    private void Instance_ErrorEvent(string message)
    {
        throw new System.NotImplementedException();
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
