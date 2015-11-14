using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamCore
{
    public delegate void DataArrivedCallback(object data);

    // SImple class for data sender
    // When new data is to be sent, SendData should be called
    public class InterModularDataSender
    {
        internal string DataLabel { get; set; }

        public void SendData(object data)
        {
            DataArrived(data);
        }

        internal DataArrivedCallback DataArrived;
    }

    // Simple class for data receiver
    // When new data arrives ReceiveData is called
    // Receiver should not modify received data
    public class InterModularDataReceiver
    {
        public DataArrivedCallback ReceiveData { get; set; }
        internal string DataLabel { get; set; }
    }


    // TODO make it thread safe / async
    // Class with set of static methods to add/remove connections between modules
    // Represents many to many connection -> when any of senders sends new data it is passed
    // to all registered receivers
    // Connection is indentified by string label
    public class InterModularConnection
    {
        #region static members

        private static List<InterModularConnection> _connections = new List<InterModularConnection>();
        
        // If connection with such label exists returns it, if not create a new one
        private static InterModularConnection GetConnection(string label)
        {
            InterModularConnection connection = null;
            foreach (var conn in _connections)
            {
                if (conn._dataLabel.Equals(label, StringComparison.Ordinal))
                {
                    connection = conn;
                }
            }

            if (connection == null)
            {
                connection = new InterModularConnection(label);
                _connections.Add(connection);
            }

            return connection;
        }

        // Function return InterModularDataSender object, through which data should be sent
        // So actual sender should save this object and use it
        public static InterModularDataSender RegisterDataSender(string label)
        {
            InterModularDataSender sender = new InterModularDataSender();
            sender.DataLabel = label;

            InterModularConnection connection = GetConnection(label);

            sender.DataArrived = connection.OnDataArrived;
            connection._senders.Add(sender);

            return sender;
        }

        // Decrements reference counter on sender, and if this connection is no longer used
        // by any object, remove it
        public static void UnregisterDataSender(InterModularDataSender sender)
        {
            InterModularConnection connection = GetConnection(sender.DataLabel);
            if(connection.RemoveSender(sender))
                _connections.Remove(connection);
        }

        // Adds receiver to sender with specified data label
        // Returns InterModularDataReceiver object, which should be saved and used later
        public static InterModularDataReceiver RegisterDataReceiver(string label, DataArrivedCallback callback, bool sendCache = false)
        {
            InterModularDataReceiver receiver = new InterModularDataReceiver();
            receiver.ReceiveData = callback;
            receiver.DataLabel = label;

            InterModularConnection connection = GetConnection(label);
            
            connection.AddReceiver(receiver, sendCache);
            return receiver;
        }
        
        // Removes receiver from connection
        // If connection is no longer used by any object, remove connection
        public static void UnregisterDataReceiver(InterModularDataReceiver receiver)
        {
            InterModularConnection connection = GetConnection(receiver.DataLabel);
            if (connection.RemoveReceiver(receiver))
                _connections.Remove(connection);
        }

        public static object GetDataCache(string label)
        {
            InterModularConnection connection = GetConnection(label);
            return connection._dataCache;
        }

        #endregion

        private List<InterModularDataSender> _senders;
        private List<InterModularDataReceiver> _receivers;
        private string _dataLabel;
        private object _dataCache = null;

        internal InterModularConnection(string label)
        {
            _senders = new List<InterModularDataSender>();
            _receivers = new List<InterModularDataReceiver>();
            _dataLabel = label;
        }

        private void OnDataArrived(object data)
        {
            foreach(var receiver in _receivers)
            {
                if (receiver.ReceiveData != null)
                    receiver.ReceiveData(data);
            }
        }

        // Adds receiver and if last data is to be sent, it is sent
        public void AddReceiver(InterModularDataReceiver receiver, bool sendCache = false)
        {
            _receivers.Add(receiver);
            if (sendCache && receiver.ReceiveData != null)
                receiver.ReceiveData(_dataCache);
        }

        // Removes receiver from connection and returns true if connection is no longer used by any object
        public bool RemoveReceiver(InterModularDataReceiver receiver)
        {
            _receivers.Remove(receiver);
            if (_receivers.Count == 0 && _senders.Count == 0)
                return true;
            return false;
        }

        // Removes sender from connection and returns true if connection is no longer used by any object
        public bool RemoveSender(InterModularDataSender sender)
        {
            _senders.Remove(sender);
            if (_receivers.Count == 0 && _senders.Count == 0)
                return true;
            return false;
        }
    }
}
