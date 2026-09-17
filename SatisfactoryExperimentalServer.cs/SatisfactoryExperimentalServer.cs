using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Engine;
using WindowsGSM.GameServer.Query;

namespace WindowsGSM.Plugins
{
    public class SatisfactoryExperimentalServer : SteamCMDAgent
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.SatisfactoryExperimentalServer", // WindowsGSM.XXXX
            author = "Sarpendon",
            description = "WindowsGSM plugin for supporting Satisfactory Dedicated Server 1.0 and above.",
            version = "1.1",
            url = "https://github.com/Sarpendon/WindowsGSM.SatisfactoryExperimentalServer", // Github repository link (Best practice)
            color = "#8802db" // Color Hex
        };

        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => true;
        public override string AppId => "1690800 -beta experimental"; // Game server appId Steam

        // - Standard Constructor and properties
        public SatisfactoryExperimentalServer(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData;

        // Error and Notice are deliberately not redeclared here. SteamCMDAgent already provides
        // both, and a field of the same name hides the base property: WindowsGSM reads
        // gameServer.Error dynamically and binds to the most derived member, so everything the
        // base wrote - the reason Install() failed, for one - never reached the UI.

        // - Game server Fixed variables
        public override string StartPath => @"Engine\Binaries\Win64\FactoryServer-Win64-Shipping.exe"; // Game server start path
        public string FullName = "Satisfactory Dedicated Server"; // Game server FullName

        // Capability flag only. WindowsGSM overwrites it with the server's own Embed Console
        // setting (MainWindow.Server_BeginStart) right before calling Start().
        public bool AllowsEmbedConsole = true;
        public int PortIncrements = 1; // This tells WindowsGSM how many ports should skip after installation

        // TODO: Undisclosed method. Satisfactory 1.0 does not answer A2S, so the player count in
        // the server list stays empty. Harmless - a failed query only skips the refresh.
        public object QueryMethod = new A2S(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()

        // - Game server default values
        public string Port = "7777"; // Default port
        public string QueryPort = "15777"; // Default query port. This is the port specified in the Server Manager in the client UI to establish a server connection.
        public string BeaconPort = "15000"; // Default beacon port. This port currently cannot be set freely.

        // TODO: Unsupported option
        public string Defaultmap = "Dedicated"; // Default map name

        // TODO: May not support
        public string Maxplayers = "4"; // Default maxplayers

        public string Additional = "-log -unattended -newconsole"; // Additional server start parameter


        // - Create a default cfg for the game server after installation
        //   No config file seems to be needed. WindowsGSM calls this after installing, so it has
        //   to exist - as a plain method rather than async void, which only produced a warning.
        public void CreateServerCFG() { }

        // - Start server function, return its Process to WindowsGSM
        public async Task<Process> Start()
        {
            string shipExePath = ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            if (!File.Exists(shipExePath))
            {
                Error = $"{Path.GetFileName(shipExePath)} not found ({shipExePath})";
                return null;
            }

            // Prepare start parameter
            string param = "FactoryGame";
            param += string.IsNullOrWhiteSpace(_serverData.ServerParam) ? string.Empty : $" {_serverData.ServerParam}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $" -Port={_serverData.ServerPort}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerQueryPort) ? string.Empty : $" -ServerQueryPort={_serverData.ServerQueryPort}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerMaxPlayer) ? string.Empty : $" -ini:Game:[/Script/Engine.GameSession]:MaxPlayers={_serverData.ServerMaxPlayer}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerIP) ? string.Empty : $" -Multihome={_serverData.ServerIP}";

            // Prepare Process
            var p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = shipExePath,
                    Arguments = param,
                    WindowStyle = ProcessWindowStyle.Normal,
                    CreateNoWindow = false,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            // Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on
            bool embedConsole = AllowsEmbedConsole;
            if (embedConsole)
            {
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;

                // Without this, non-ASCII characters in server and player names arrive mangled
                // in the WindowsGSM console pane.
                p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                p.StartInfo.StandardErrorEncoding = Encoding.UTF8;

                var serverConsole = new ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;
            }

            // Start Process
            try
            {
                p.Start();
                if (embedConsole)
                {
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                }

                return p;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null; // return null if fail to start
            }
        }

        // - Graceful shutdown support
        //
        // The previous stop path sent Ctrl+C to the process window with SendKeys. That never
        // reached the server: WindowsGSM hides the server window immediately after starting it
        // (MainWindow.Server_BeginStart), so SetForegroundWindow fails on a hidden window, and
        // SendKeys.SendWait is global - the keystroke went to whatever window happened to have
        // focus on the host machine. The server was then killed by WindowsGSM rather than shut
        // down, so the world was only ever as current as its last autosave.
        //
        // FactoryServer is a console application, so attaching to its console and raising
        // CTRL_C_EVENT there reaches it directly and lets Unreal run its shutdown, which saves.

        private delegate bool ConsoleCtrlDelegate(uint ctrlType);

        private const uint CTRL_C_EVENT = 0;

        // Saving a large factory takes noticeably longer than the previous 20 seconds allowed.
        // Server_BeginStop awaits Stop() without a timeout of its own, so this is honoured.
        private const int GRACEFUL_EXIT_TIMEOUT_MS = 120000;
        private const int FORCED_EXIT_TIMEOUT_MS = 5000;

        // WindowsGSM clears the console pane as soon as Stop() returns, taking the shutdown
        // output with it. Set to 0 to hand back immediately.
        private const int CONSOLE_LINGER_MS = 3000;

        // Console attachment is per process, not per thread, and WindowsGSM can run two stops at
        // once (auto restart, restart crontab and update-on-start each drive their own timer).
        // Without this lock one stop can detach the console another is about to signal.
        private static readonly object _consoleSignalLock = new object();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handler, bool add);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

        // - Stop server function
        public async Task Stop(Process p)
        {
            if (p == null) { return; }

            await Task.Run(() =>
            {
                // Note: WindowsGSM builds the instance behind Stop() without a ServerConfig, so
                // _serverData is null in here. Everything this method needs comes from the
                // process itself.
                try
                {
                    if (p.HasExited) { return; }
                }
                catch (Exception e)
                {
                    Error = e.Message;
                    return;
                }

                lock (_consoleSignalLock)
                {
                    bool attached = false;
                    bool signalled = false;

                    try
                    {
                        // AttachConsole fails while this process still owns a console of its own.
                        FreeConsole();

                        attached = AttachConsole((uint)p.Id);
                        if (attached)
                        {
                            // The event reaches every process on that console, which now includes
                            // WindowsGSM. Ignore it here first, or the manager goes down with the
                            // server it is trying to stop.
                            SetConsoleCtrlHandler(null, true);
                            signalled = GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0);
                        }

                        if (!signalled && p.MainWindowHandle != IntPtr.Zero)
                        {
                            // Fallback for a server that really does have a visible window.
                            // Guarded on the handle because SendKeys is global: with no window to
                            // bring forward, the keystroke lands in whatever the user is working in.
                            ServerConsole.SetMainWindow(p.MainWindowHandle);
                            ServerConsole.SendWaitToMainWindow("^c");
                            signalled = true;
                        }

                        p.WaitForExit(signalled ? GRACEFUL_EXIT_TIMEOUT_MS : FORCED_EXIT_TIMEOUT_MS);

                        if (signalled && CONSOLE_LINGER_MS > 0 && p.HasExited)
                        {
                            Thread.Sleep(CONSOLE_LINGER_MS);
                        }
                    }
                    catch (Exception e)
                    {
                        Error = e.Message;
                    }
                    finally
                    {
                        if (attached)
                        {
                            try { SetConsoleCtrlHandler(null, false); } catch { /* ignore */ }
                            try { FreeConsole(); } catch { /* ignore */ }
                        }
                    }
                }
            });
        }

        // fixes WinGSM bug, https://github.com/WindowsGSM/WindowsGSM/issues/57#issuecomment-983924499
        public new async Task<Process> Update(bool validate = false, string custom = null)
        {
            var (p, error) = await Installer.SteamCMD.UpdateEx(serverData.ServerID, AppId, validate, custom: custom, loginAnonymous: loginAnonymous);
            Error = error;

            // UpdateEx hands back null when steamcmd could not be downloaded or the Steam account
            // is not set up. The old code dereferenced it unconditionally, so a failed update
            // threw a NullReferenceException out of WindowsGSM's update path - twice, since the
            // retry hits the same line - instead of reporting the error it was given.
            if (p == null) { return null; }

            // Auto Update restarts the server as soon as this returns, so the update has to be
            // finished by then, not merely started.
            await Task.Run(() => p.WaitForExit());
            return p;
        }
    }
}
