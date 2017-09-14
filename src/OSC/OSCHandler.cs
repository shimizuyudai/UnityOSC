//
//	  UnityOSC - Open Sound Control interface for the Unity3d game engine	  
//
//	  Copyright (c) 2012 Jorge Garcia Martin
//	  Last edit: Gerard Llorach 2nd August 2017
//
// 	  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// 	  documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// 	  the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// 	  and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// 	  The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// 	  of the Software.
//
// 	  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// 	  TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// 	  THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// 	  CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// 	  IN THE SOFTWARE.
//
//	  Inspired by http://www.unifycommunity.com/wiki/index.php?title=AManagerClass

using System;
using System.Net;
using System.Collections.Generic;

using UnityEngine;
using UnityOSC;

/// <summary>
/// Models a log of a server composed by an OSCServer, a List of OSCPacket and a List of
/// strings that represent the current messages in the log.
/// </summary>
public struct ServerLog
{
	public OSCServer server;
	public List<OSCPacket> packets;
	public List<string> log;
}

/// <summary>
/// Models a log of a client composed by an OSCClient, a List of OSCMessage and a List of
/// strings that represent the current messages in the log.
/// </summary>
public struct ClientLog
{
	public OSCClient client;
	public List<OSCMessage> messages;
	public List<string> log;
}

/// <summary>
/// Handles all the OSC servers and clients of the current Unity game/application.
/// Tracks incoming and outgoing messages.
/// </summary>
public class OSCHandler : MonoBehaviour
{


	[Serializable]
	public class ClientInfo{
		[SerializeField]
		string name;
		public string Name{
			get{
				return name;
			}
		}

		[SerializeField]
		string ipAddress;
		public string IPAddress{
			get{
				return ipAddress;
			}
		}
		[SerializeField]
		int port;
		public int Port{
			get{
				return port;
			}
		}
	}

	[Serializable]
	public class ServerInfo{
		[SerializeField]
		string name;
		public string Name{
			get{
				return name;
			}
		}

		[SerializeField]
		int port;
		public int Port{
			get{
				return port;
			}
		}
	}

//	public class PacketInfo
//	{
//		OSCServer server;
//		public OSCServer Server{
//			get{
//				return server;
//			}
//		}
//		OSCPacket packet;
//		public OSCPacket Packet{
//			get{
//				return packet;
//			}
//		}
//
//		public PacketInfo(OSCServer server, OSCPacket packet)
//		{
//			this.server = server;
//			this.packet = packet;
//		}
//	}

	public delegate void OnOSCMessageReceived (OSCMessage message);

	public event OnOSCMessageReceived AsyncPacketReceivedEvent;
	public event OnOSCMessageReceived SynchronizedPacketReceivedEvent;
	private System.Collections.Queue queue;

	void Awake()
	{
		instance = this;
		queue = new System.Collections.Queue();
		queue = System.Collections.Queue.Synchronized(queue);
		Init ();
	}


	void OnPacketReceived(OSCServer server, OSCPacket packet)
	{
		if(AsyncPacketReceivedEvent != null){
			if (packet.IsBundle ()) {
				// OSCBundleの場合
				OSCBundle bundle = packet as OSCBundle;
				foreach (OSCMessage msg in bundle.Data) {
					AsyncPacketReceivedEvent (msg);
				}
			} else {
				OSCMessage msg = packet as OSCMessage;
				AsyncPacketReceivedEvent (msg);
			}
		}
		queue.Enqueue (packet);
	}

	void Update()
	{
		if (SynchronizedPacketReceivedEvent != null) {
			while (0 < queue.Count) {
				OSCPacket packet = queue.Dequeue () as OSCPacket;
				if (packet.IsBundle ()) {
					OSCBundle bundle = packet as OSCBundle;
					foreach (OSCMessage msg in bundle.Data) {
						SynchronizedPacketReceivedEvent (msg);
					}
				} else {
					OSCMessage msg = packet as OSCMessage;
					SynchronizedPacketReceivedEvent (msg);
				}
			}
		}
	}

	private static OSCHandler instance;

	public static OSCHandler Instance 
	{
	    get 
		{
			return instance;
	    }
	}
	
	#region Member Variables
	private Dictionary<string, ClientLog> _clients = new Dictionary<string, ClientLog>();
	private Dictionary<string, ServerLog> _servers = new Dictionary<string, ServerLog>();
    public List<OSCPacket> packets = new List<OSCPacket>();

	
	private const int _loglength = 100;
	#endregion

	[SerializeField]
	ClientInfo[] clientInfos;
	[SerializeField]
	ServerInfo[] serverInfos;

	public void Init()
	{
        //Initialize OSC clients (transmitters)
        //Example:		
		foreach(var clientInfo in clientInfos){
			CreateClient(clientInfo.Name, IPAddress.Parse(clientInfo.IPAddress), clientInfo.Port);
		}

		foreach(var serverInfo in serverInfos)
		{
			CreateServer(serverInfo.Name, serverInfo.Port);
		}

        //
    }

    #region Properties
    public Dictionary<string, ClientLog> Clients
	{
		get
		{
			return _clients;
		}
	}
	
	public Dictionary<string, ServerLog> Servers
	{
		get
		{
			return _servers;
		}
	}
	#endregion
	
	#region Methods
	
	/// <summary>
	/// Ensure that the instance is destroyed when the game is stopped in the Unity editor
	/// Close all the OSC clients and servers
	/// </summary>
	void OnDestroy() 
	{
		foreach(KeyValuePair<string,ClientLog> pair in _clients)
		{
			pair.Value.client.Close();
		}
		
		foreach(KeyValuePair<string,ServerLog> pair in _servers)
		{
			pair.Value.server.Close();
		}
		instance = null;
	}
	
	/// <summary>
	/// Creates an OSC Client (sends OSC messages) given an outgoing port and address.
	/// </summary>
	/// <param name="clientId">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="destination">
	/// A <see cref="IPAddress"/>
	/// </param>
	/// <param name="port">
	/// A <see cref="System.Int32"/>
	/// </param>
	public void CreateClient(string clientId, IPAddress destination, int port)
	{
		ClientLog clientitem = new ClientLog();
		clientitem.client = new OSCClient(destination, port);
		clientitem.log = new List<string>();
		clientitem.messages = new List<OSCMessage>();
		
		_clients.Add(clientId, clientitem);
		
		// Send test message
		string testaddress = "/test/alive/";
		OSCMessage message = new OSCMessage(testaddress, destination.ToString());
		message.Append(port); message.Append("OK");
		
		_clients[clientId].log.Add(String.Concat(DateTime.UtcNow.ToString(),".",
		                                         FormatMilliseconds(DateTime.Now.Millisecond), " : ",
		                                         testaddress," ", DataToString(message.Data)));
		_clients[clientId].messages.Add(message);
		
		_clients[clientId].client.Send(message);
	}
	
	/// <summary>
	/// Creates an OSC Server (listens to upcoming OSC messages) given an incoming port.
	/// </summary>
	/// <param name="serverId">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="port">
	/// A <see cref="System.Int32"/>
	/// </param>
	public OSCServer CreateServer(string serverId, int port)
	{
        OSCServer server = new OSCServer(port);
        server.PacketReceivedEvent += OnPacketReceived;

        ServerLog serveritem = new ServerLog();
        serveritem.server = server;
		serveritem.log = new List<string>();
		serveritem.packets = new List<OSCPacket>();
		
		_servers.Add(serverId, serveritem);

        return server;
	}

    /// <summary>
    /// Callback when a message is received. It stores the messages in a list of the oscControl
    


	/// <summary>
	/// Sends an OSC message to a specified client, given its clientId (defined at the OSC client construction),
	/// OSC address and a single value. Also updates the client log.
	/// </summary>
	/// <param name="clientId">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="address">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="value">
	/// A <see cref="T"/>
	/// </param>
	public void SendMessageToClient<T>(string clientId, string address, T value)
	{
		List<object> temp = new List<object>();
		temp.Add(value);
		
		SendMessageToClient(clientId, address, temp);
	}
	
	/// <summary>
	/// Sends an OSC message to a specified client, given its clientId (defined at the OSC client construction),
	/// OSC address and a list of values. Also updates the client log.
	/// </summary>
	/// <param name="clientId">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="address">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="values">
	/// A <see cref="List<T>"/>
	/// </param>
	public void SendMessageToClient<T>(string clientId, string address, List<T> values)
	{	
		if(_clients.ContainsKey(clientId))
		{
			OSCMessage message = new OSCMessage(address);
		
			foreach(T msgvalue in values)
			{
				message.Append(msgvalue);
			}
			
			if(_clients[clientId].log.Count < _loglength)
			{
				_clients[clientId].log.Add(String.Concat(DateTime.UtcNow.ToString(),".",
				                                         FormatMilliseconds(DateTime.Now.Millisecond),
				                                         " : ", address, " ", DataToString(message.Data)));
				_clients[clientId].messages.Add(message);
			}
			else
			{
				_clients[clientId].log.RemoveAt(0);
				_clients[clientId].messages.RemoveAt(0);
				
				_clients[clientId].log.Add(String.Concat(DateTime.UtcNow.ToString(),".",
				                                         FormatMilliseconds(DateTime.Now.Millisecond),
				                                         " : ", address, " ", DataToString(message.Data)));
				_clients[clientId].messages.Add(message);
			}
			
			_clients[clientId].client.Send(message);
		}
		else
		{
			Debug.LogError(string.Format("Can't send OSC messages to {0}. Client doesn't exist.", clientId));
		}
	}
	
	/// <summary>
	/// Updates clients and servers logs.
    /// NOTE: Only used by the editor helper script (OSCHelper.cs), could be removed
	/// </summary>
	public void UpdateLogs()
	{
		foreach(KeyValuePair<string,ServerLog> pair in _servers)
		{
			if(_servers[pair.Key].server.LastReceivedPacket != null)
			{
				//Initialization for the first packet received
				if(_servers[pair.Key].log.Count == 0)
				{	
					_servers[pair.Key].packets.Add(_servers[pair.Key].server.LastReceivedPacket);
						
					_servers[pair.Key].log.Add(String.Concat(DateTime.UtcNow.ToString(), ".",
					                                         FormatMilliseconds(DateTime.Now.Millisecond)," : ",
					                                         _servers[pair.Key].server.LastReceivedPacket.Address," ",
					                                         DataToString(_servers[pair.Key].server.LastReceivedPacket.Data)));
					break;
				}
						
				if(_servers[pair.Key].server.LastReceivedPacket.TimeStamp
				   != _servers[pair.Key].packets[_servers[pair.Key].packets.Count - 1].TimeStamp)
				{	
					if(_servers[pair.Key].log.Count > _loglength - 1)
					{
						_servers[pair.Key].log.RemoveAt(0);
						_servers[pair.Key].packets.RemoveAt(0);
					}
		
					_servers[pair.Key].packets.Add(_servers[pair.Key].server.LastReceivedPacket);
						
					_servers[pair.Key].log.Add(String.Concat(DateTime.UtcNow.ToString(), ".",
					                                         FormatMilliseconds(DateTime.Now.Millisecond)," : ",
					                                         _servers[pair.Key].server.LastReceivedPacket.Address," ",
					                                         DataToString(_servers[pair.Key].server.LastReceivedPacket.Data)));
				}
			}
		}
	}
	
	/// <summary>
	/// Converts a collection of object values to a concatenated string.
	/// </summary>
	/// <param name="data">
	/// A <see cref="List<System.Object>"/>
	/// </param>
	/// <returns>
	/// A <see cref="System.String"/>
	/// </returns>
	private string DataToString(List<object> data)
	{
		string buffer = "";
		
		for(int i = 0; i < data.Count; i++)
		{
			buffer += data[i].ToString() + " ";
		}
		
		buffer += "\n";
		
		return buffer;
	}
	
	/// <summary>
	/// Formats a milliseconds number to a 000 format. E.g. given 50, it outputs 050. Given 5, it outputs 005
	/// </summary>
	/// <param name="milliseconds">
	/// A <see cref="System.Int32"/>
	/// </param>
	/// <returns>
	/// A <see cref="System.String"/>
	/// </returns>
	private string FormatMilliseconds(int milliseconds)
	{	
		if(milliseconds < 100)
		{
			if(milliseconds < 10)
				return String.Concat("00",milliseconds.ToString());
			
			return String.Concat("0",milliseconds.ToString());
		}
		
		return milliseconds.ToString();
	}
			
	#endregion
}	

