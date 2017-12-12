using System;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Protocol.Heartbeat
{
    public interface IConnectable
    {
        event EventHandler<BoolEventArgs> OnConnectedStateChanged;
        bool IsConnected { get; }
        Heartbeat Heartbeat { get; }
        void Connect();
        void Disconnect();
    }

}