using CommandLine;
using EV3.Transmitter.Services;
using Grpc.Core;
using Lego.Ev3.Core;
using Lego.Ev3.Desktop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EV3.Transmitter
{

    public class MyBrick
    {
        public bool BluetoothConnected { get; private set; }
        private Brick Brick { get; }
        public string Id { get; }
        public string Port { get; }

        public bool RemoteConnected => RegisteredBrick != null;
        private RegisteredBrickResponse RegisteredBrick { get; set; }

        public MyBrick(string port)
        {
            this.Port = port;
            this.Brick = new Brick(new BluetoothCommunication(port, 1000));
            this.Id = Guid.NewGuid().ToString().Split("-")[3].ToUpper();
        }


        public async Task<bool> ConnectAsync()
        {
            try
            {
                Log.Information("Connecting to {0} ({1})...", Port, Id);
                if (await Brick.ConnectAsync())
                {
                    BluetoothConnected = true;
                    await UpdateUIAsync();
                    Log.Information("Connected to {0} ({1})!", Port, Id);
                    await PlayBluetoothConnectToneAsync();

                    await Brick.DirectCommand.SetLedPatternAsync(LedPattern.Orange);

                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error connecting to Brick {Port} ({Id}).");
                BluetoothConnected = false;
            }
            return false;
        }

        private async Task PlayBluetoothConnectToneAsync()
        {
            await Brick.DirectCommand.PlayToneAsync(25, 150, 200);
            Thread.Sleep(150);
            await Brick.DirectCommand.PlayToneAsync(25, 450, 200);
        }

        private async Task PlayRemoteConnectToneAsync()
        {
            await Brick.DirectCommand.PlayToneAsync(25, 700, 200);
        }

        public async Task UpdateUIAsync()
        {
            var command = Brick.CreateBatchCommand();
            command.DrawFillWindow(Color.Foreground, 0, Brick.LcdHeight);
            command.DrawRectangle(Color.Background, 0, 0, Brick.LcdWidth, Brick.LcdHeight, true);
            command.DrawText(Color.Foreground, 5, 15, "Bluetooth: " + (BluetoothConnected ? "Connected" : "Disconnected"));
            command.DrawText(Color.Foreground, 5, 30, "Remote: " + (RemoteConnected ? "Connected" : "Disconnected"));

            if(RemoteConnected)
            {
                command.DrawLine(Color.Foreground, 0, 45, Brick.LcdWidth, 45);
                command.DrawText(Color.Foreground, 5, 50, "Brick ID: " + RegisteredBrick.BrickId);
                command.DrawText(Color.Foreground, 5, 60, "Account ID: " + RegisteredBrick.AccountId);
                command.DrawText(Color.Foreground, 5, 70, "Enabled: " + RegisteredBrick.Enabled);
            }
            command.UpdateUI();
            await Brick.ExecuteBatch(command);
        }

        public async Task RegisterRemoteAsync(RegisteredBrickResponse newBrickResponse)
        {
            RegisteredBrick = newBrickResponse;
            await UpdateUIAsync();
            await PlayRemoteConnectToneAsync();
        }

        public void Disconnect()
        {
            Brick.Disconnect();
        }
    }
    class Program
    {

        public static List<MyBrick> Bricks { get; } = new List<MyBrick>();

        public static Bluetooth Bluetooth { get; } = new Bluetooth();

        public static Channel Channel { get; set; }
        public static BrickService.BrickServiceClient BrickClient { get; set; }



        static async Task ConnectToAllFoundBricks(int retryConnectionCount = 2, bool clearScreen = true, bool playTone = true, bool setLedPatten = true)
        {

            Log.Information("Searching System for Bluetooth serial ports...");
            await Bluetooth.GetBluetoothCOMPorts();
            Log.Information($"Found {Bluetooth.COMPorts.Count} Bluetooth serial ports.");
            var validDevices = Bluetooth.COMPorts;
            Log.Information($"Found {validDevices.Count} 'valid devices' (name prefix has matched).");


            var brickRegister = BrickClient.RegisterBricks();

            var hostBrickRegisterResponder = Task.Run(async () =>
            {
                while (await brickRegister.ResponseStream.MoveNext())
                {
                    var newBrickResponse = brickRegister.ResponseStream.Current;
                    var brick = Bricks.SingleOrDefault(x => x.Id == newBrickResponse.BrickId);
                    if (brick == null) continue;

                    await brick.RegisterRemoteAsync(newBrickResponse);
                }
            });

            foreach (var port in validDevices)
            {
                var brick = new MyBrick(port.Port);
                for (int i = 0; i < retryConnectionCount; i++)
                {

                    var connect = brick.ConnectAsync();
                    connect.Wait();
                    if (connect.Result)
                    {
                        Bricks.Add(brick);
                        brickRegister.RequestStream.WriteAsync(new NewBrickRequest
                        {
                            BrickId = brick.Id
                        }).Wait();
                        break;
                    }
                    else
                    {
                        Log.Warning($"Connection failed. (Attempt {i + 1} of {retryConnectionCount})");
                        continue;
                    }

                }
            }//);

            await brickRegister.RequestStream.CompleteAsync();
            await hostBrickRegisterResponder;


        }

        static async Task<bool> ConnectToHost(string host)
        {
            Log.Information("Connecting to host GRPC service");
            var channel = new Channel(host, 5000, ChannelCredentials.Insecure);
            var client = new HealthService.HealthServiceClient(channel);
            try
            {
                var response = await client.CheckConnectionAsync(new HealthRequest() { Datetime = DateTime.Now.ToString() });
                BrickClient = new BrickService.BrickServiceClient(channel);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error: {0}");
                return false;
            }

        }


        static async Task ProccessCommandsFromServer()
        {
            Console.ReadLine();
        }

        public static async Task Application(Options options)
        {

            if (await ConnectToHost(options.Host))
            {
                await ConnectToAllFoundBricks();

                await ProccessCommandsFromServer();
            }

            foreach (var brick in Bricks)
            {
                brick.Disconnect();
            }


        }

        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            Options options = null;
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       options = o;
                   });

            await Application(options);
        }



        static void OnBrickChanged(object sender, BrickChangedEventArgs e)
        {
            Console.WriteLine("Brick Updated...");
            Console.WriteLine("-------- Buttons --------");
            Console.WriteLine("Enter:", e.Buttons.Enter);
            Console.WriteLine("Up:", e.Buttons.Up);
            Console.WriteLine("Down:", e.Buttons.Down);
            Console.WriteLine("Left:", e.Buttons.Left);
            Console.WriteLine("Right:", e.Buttons.Right);
            Console.WriteLine("Back:", e.Buttons.Back);
            Console.WriteLine("-------- Ports --------");
            foreach (var port in e.Ports)
            {
                Console.WriteLine("{0}({1}): {2}", port.Value.Name, port.Value.Type.ToString(), port.Value.RawValue);
            }


        }

    }
}
