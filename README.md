# WindowsGSM.SatisfactoryExperimentalServer
🧩 WindowsGSM plugin that provides Satisfactory Experimental Dedicated server support!

🏷️ To be used with https://windowsgsm.com/ 


# Basic Installation: 
1. Download  WindowsGSM from the Link above.
2. Download this Plugin as .zip container and don't unpack it.
3. Create a Folder at a Location you wan't all Server to be Installed and Run.
4. Drag WindowsGSM.Exe into previoulsy created folder and execute it.
5. Press on the Puzzle Icon in the left bottom side and install this plugin by navigating to it and select the Zip File.
6. Wait a couple of seconds then close the plugin menu and install the game server.


# The Game:
- 🕹️ **Steam Site:** https://store.steampowered.com/app/526870/Satisfactory/
- 📁 **Homepage:** https://www.satisfactorygame.com/

# Requirements:
- 🖥️ **WindowsGSM** >= 1.21.0

# Server Settings:
> [!IMPORTANT]
>- **Server IP Adress:** *Local IP of your Server there is no need to change this GSM should get the right IP adress itself*
>- **Server Port:** *Game Port of the Server*
>- **Server Maxplayer:** *Maximum Number of Players on your Server*
>- **Server Start Param:** *Some Parameter are already filled in by default you can add or remove them as you wish! 

# Changelog:
### 1.1
- **Stopping the server now reaches it.** The stop signal was sent to the server's window with
  SendKeys, but WindowsGSM hides that window right after starting the server - so the keystroke
  went to whatever window happened to have focus on the machine, never to the server. Every stop
  ran into the timeout and ended in a hard kill. The signal is now raised on the server's own
  console, so it shuts down properly instead of being killed.
- The shutdown output stays readable for a few seconds instead of being cleared instantly.
- **Failed installs and updates now say why.** The reason was being swallowed and shown as an
  empty `[ERROR]`; a failed update additionally crashed with a `NullReferenceException`.
- A missing server executable is reported as such instead of a generic Windows error.
- Console output is read as UTF-8, so umlauts and other non-ASCII characters are no longer mangled.

# Other WinGSM Plugins:
| Icon | Game Name | Link | Version |
| --- | --- | --- | --- |
| <img src="https://i.imgur.com/LI1uPIJ.png" width="100" height="100"> | Myth of Empires Dedicated Server | [GitHub Link](https://github.com/Sarpendon/WindowsGSM.MythofEmpires) | 2.0 |
| <img src="https://i.imgur.com/25x4Ohs.png" width="100" height="100"> | Valheim Dedicated Server | [GitHub Link](https://github.com/Sarpendon/WindowsGSM.Valheim) | 1.2 |
| <img src="https://i.imgur.com/A9jtLPQ.png" width="100" height="100"> | V Rising Dedicated Server | [GitHub Link](https://github.com/Sarpendon/WindowsGSM.VRising) | 1.1 |
| <img src="https://i.imgur.com/A6dCSy9.png" width="100" height="100"> | Life is Feudal Dedicated Server | [GitHub Link](https://github.com/Sarpendon/WindowsGSM.LifeIsFeudal) | 1.2 |
