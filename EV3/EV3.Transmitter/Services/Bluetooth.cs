using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EV3.Transmitter.Services
{
    public class Bluetooth
    {
        public ObservableCollection<COMPort> COMPorts { get; set; } = new ObservableCollection<COMPort>();
        public bool CanRefresh { get; set; } = false;

        public async Task GetBluetoothCOMPorts()
        {

            COMPorts.Clear();
            CanRefresh = false;

            var ports = System.IO.Ports.SerialPort.GetPortNames();
            foreach (var port in ports)
            {
                COMPorts.Add(new COMPort(port));//, Direction.UNDEFINED, ""));
            }
            

            CanRefresh = true;

            await Task.CompletedTask;
        }
    }



    public class COMPort : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _port;
        public string Port
        {
            get => _port;
            set
            {
                _port = value;
                RaisePropertyChanged();
            }
        }

        //private Direction _direction;
        //public Direction Direction
        //{
        //    get => _direction;
        //    set
        //    {
        //        _direction = value;
        //        RaisePropertyChanged();
        //    }
        //}

        //private string _name;
        //public string Name
        //{
        //    get => _name;
        //    set
        //    {
        //        _name = value;
        //        RaisePropertyChanged();
        //    }
        //}

        public COMPort(string port)//, Direction direction, string name)
        {
            Port = port;
            //this.Direction = direction;
            //Name = name;
        }
    }

    public enum Direction
    {
        UNDEFINED,
        INCOMING,
        OUTGOING
    }

}
