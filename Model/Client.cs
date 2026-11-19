using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using _4RTools.Utils;

namespace _4RTools.Model
{
    public class ClientDTO
    {
        public int index { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string hpAddress { get; set; }
        public string nameAddress { get; set; }

        public int hpAddressPointer { get; set; }
        public int nameAddressPointer { get; set; }

        public ClientDTO() { }

        public ClientDTO(string name, string description, string hpAddress, string nameAddress)
        {
            this.name = name;
            this.description = description;
            this.hpAddress = hpAddress;
            this.nameAddress = nameAddress;

            this.hpAddressPointer = Convert.ToInt32(hpAddress, 16);
            this.nameAddressPointer = Convert.ToInt32(nameAddress, 16);
        }
    }

    public sealed class ClientListSingleton
    {
        private static readonly object SyncRoot = new object();
        private static readonly List<Client> clients = new List<Client>();

        public static void AddClient(Client c)
        {
            if (c == null)
            {
                return;
            }

            lock (SyncRoot)
            {
                clients.Add(c);
            }
        }

        public static void RemoveClient(Client c)
        {
            if (c == null)
            {
                return;
            }

            lock (SyncRoot)
            {
                clients.Remove(c);
            }
        }

        public static List<Client> GetAll()
        {
            lock (SyncRoot)
            {
                return new List<Client>(clients);
            }
        }

        public static bool ExistsByProcessName(string processName)
        {
            lock (SyncRoot)
            {
                return clients.Exists(client => string.Equals(client.processName, processName, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    public sealed class ClientSingleton
    {
        private static readonly object SyncRoot = new object();
        private static Client client;

        private ClientSingleton(Client newClient)
        {
            Client previous = null;
            lock (SyncRoot)
            {
                if (!ReferenceEquals(client, newClient))
                {
                    previous = client;
                    client = newClient;
                }
            }

            if (previous != null)
            {
                previous.Dispose();
            }
        }

        public static ClientSingleton Instance(Client newClient)
        {
            return new ClientSingleton(newClient);
        }

        public static Client GetClient()
        {
            lock (SyncRoot)
            {
                return client;
            }
        }

        public static void Clear()
        {
            Client previous;
            lock (SyncRoot)
            {
                previous = client;
                client = null;
            }

            if (previous != null)
            {
                previous.Dispose();
            }
        }
    }

    public class Client : IDisposable
    {
        public Process process { get; private set; }
        public string processName { get; private set; }
        private ProcessMemoryReader PMR { get; set; }
        public int currentNameAddress { get; set; }
        public int currentHPBaseAddress { get; set; }
        private int statusBufferAddress { get; set; }
        public bool IsSupported { get; private set; }
        private bool disposed = false;

        public bool HasMemorySupport
        {
            get
            {
                return !this.disposed
                    && this.currentHPBaseAddress != 0
                    && this.PMR != null
                    && this.PMR.IsOpen;
            }
        }

        public Client(string processName, int currentHPBaseAddress, int currentNameAddress)
        {
            this.currentNameAddress = currentNameAddress;
            this.currentHPBaseAddress = currentHPBaseAddress;
            this.processName = processName;
            this.statusBufferAddress = currentHPBaseAddress + 0x474;
            this.IsSupported = currentHPBaseAddress != 0;
        }

        public Client(ClientDTO dto)
        {
            this.processName = dto.name;
            this.currentHPBaseAddress = Convert.ToInt32(dto.hpAddress, 16);
            this.currentNameAddress = Convert.ToInt32(dto.nameAddress, 16);
            this.statusBufferAddress = this.currentHPBaseAddress + 0x474;
            this.IsSupported = this.currentHPBaseAddress != 0;
        }

        public Client(string processDisplayName)
        {
            this.PMR = new ProcessMemoryReader();
            this.IsSupported = false;

            if (string.IsNullOrWhiteSpace(processDisplayName))
            {
                return;
            }

            const string separator = ".exe - ";
            int sepIndex = processDisplayName.LastIndexOf(separator, StringComparison.OrdinalIgnoreCase);
            if (sepIndex <= 0)
            {
                Logger.Warn("Client", "Unexpected process entry format: " + processDisplayName);
                return;
            }

            string rawProcessName = processDisplayName.Substring(0, sepIndex);
            string pidText = processDisplayName.Substring(sepIndex + separator.Length);
            int chosenPID;
            if (!int.TryParse(pidText, out chosenPID))
            {
                Logger.Warn("Client", "Could not parse PID from process entry: " + processDisplayName);
                return;
            }

            this.processName = rawProcessName;
            Process[] matchingProcesses = Process.GetProcessesByName(rawProcessName);

            foreach (Process candidate in matchingProcesses)
            {
                if (chosenPID == candidate.Id && this.process == null)
                {
                    this.process = candidate;
                    this.PMR.ReadProcess = candidate;
                    this.PMR.OpenProcess();

                    Client matchedTemplate = GetClientByProcess(rawProcessName);
                    if (matchedTemplate != null)
                    {
                        this.currentHPBaseAddress = matchedTemplate.currentHPBaseAddress;
                        this.currentNameAddress = matchedTemplate.currentNameAddress;
                        this.statusBufferAddress = matchedTemplate.statusBufferAddress;
                        this.IsSupported = true;
                    }
                    else
                    {
                        // Do not block spammers/macros for non-supported versions.
                        this.currentHPBaseAddress = 0;
                        this.currentNameAddress = 0;
                        this.statusBufferAddress = 0;
                        this.IsSupported = false;
                    }
                }
                else
                {
                    candidate.Dispose();
                }
            }
        }

        public bool IsProcessAlive()
        {
            if (this.disposed || this.process == null)
            {
                return false;
            }

            try
            {
                return !this.process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        public IntPtr GetWindowHandle()
        {
            if (!IsProcessAlive())
            {
                return IntPtr.Zero;
            }

            try
            {
                return this.process.MainWindowHandle;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        public bool IsGameWindowFocused()
        {
            IntPtr hWnd = GetWindowHandle();
            if (hWnd == IntPtr.Zero)
            {
                return false;
            }

            return Interop.GetForegroundWindow() == hWnd;
        }

        public bool ShouldPauseForFocus()
        {
            Profile currentProfile = ProfileSingleton.GetCurrent();
            if (currentProfile != null && currentProfile.UserPreferences != null && currentProfile.UserPreferences.pauseWhenUnfocused)
            {
                return !IsGameWindowFocused();
            }

            return false;
        }

        public bool ShouldPauseAutomation()
        {
            return ShouldPauseForFocus() || ChatPauseState.IsChatting;
        }

        public bool ShouldPauseAutopotKey(System.Windows.Input.Key key)
        {
            if (ShouldPauseForFocus())
            {
                return true;
            }

            if (ChatPauseState.IsChatting && ChatPauseState.IsTypingKey(key))
            {
                return true;
            }

            return false;
        }

        private string ReadMemoryAsString(int address)
        {
            if (this.PMR == null || !this.PMR.IsOpen || address == 0)
            {
                return string.Empty;
            }

            byte[] bytes = new byte[40];
            int bytesRead;
            if (!this.PMR.ReadProcessMemory((IntPtr)address, bytes, bytes.Length, out bytesRead))
            {
                return string.Empty;
            }

            int length = 0;
            while (length < bytesRead && bytes[length] != 0)
            {
                length++;
            }

            return length > 0 ? Encoding.Default.GetString(bytes, 0, length) : string.Empty;
        }

        private uint ReadMemory(int address)
        {
            if (this.PMR == null || !this.PMR.IsOpen || address == 0)
            {
                return 0;
            }

            byte[] buffer = new byte[4];
            int bytesRead;
            if (!this.PMR.ReadProcessMemory((IntPtr)address, buffer, 4, out bytesRead) || bytesRead < 4)
            {
                return 0;
            }

            return BitConverter.ToUInt32(buffer, 0);
        }

        public bool TryReadVitals(byte[] scratchBuffer16, out uint currentHp, out uint maxHp, out uint currentSp, out uint maxSp)
        {
            currentHp = 0;
            maxHp = 0;
            currentSp = 0;
            maxSp = 0;

            if (!HasMemorySupport || scratchBuffer16 == null || scratchBuffer16.Length < Constants.HP_SP_BLOCK_BYTE_SIZE)
            {
                return false;
            }

            int bytesRead;
            if (!this.PMR.ReadProcessMemory((IntPtr)this.currentHPBaseAddress, scratchBuffer16, Constants.HP_SP_BLOCK_BYTE_SIZE, out bytesRead)
                || bytesRead < Constants.HP_SP_BLOCK_BYTE_SIZE)
            {
                return false;
            }

            currentHp = BitConverter.ToUInt32(scratchBuffer16, 0);
            maxHp = BitConverter.ToUInt32(scratchBuffer16, 4);
            currentSp = BitConverter.ToUInt32(scratchBuffer16, 8);
            maxSp = BitConverter.ToUInt32(scratchBuffer16, 12);
            return true;
        }

        public int ReadBuffStatusBuffer(byte[] rawBuffer, uint[] destination)
        {
            if (!HasMemorySupport || this.statusBufferAddress == 0 || rawBuffer == null || destination == null)
            {
                return 0;
            }

            int count = Math.Min(destination.Length, Constants.MAX_BUFF_LIST_INDEX_SIZE);
            int byteSize = count * sizeof(uint);
            if (rawBuffer.Length < byteSize)
            {
                return 0;
            }

            int bytesRead;
            if (!this.PMR.ReadProcessMemory((IntPtr)this.statusBufferAddress, rawBuffer, byteSize, out bytesRead) || bytesRead < sizeof(uint))
            {
                return 0;
            }

            int entriesRead = bytesRead / sizeof(uint);
            Buffer.BlockCopy(rawBuffer, 0, destination, 0, entriesRead * sizeof(uint));
            return entriesRead;
        }

        public static bool IsBelowPercent(uint current, uint max, int percent)
        {
            if (max == 0 || percent <= 0)
            {
                return false;
            }

            return (ulong)current * 100UL < (ulong)percent * (ulong)max;
        }

        public bool IsHpBelow(int percent)
        {
            return IsBelowPercent(ReadCurrentHp(), ReadMaxHp(), percent);
        }

        public bool IsSpBelow(int percent)
        {
            return IsBelowPercent(ReadCurrentSp(), ReadMaxSp(), percent);
        }

        public uint ReadCurrentHp()
        {
            return ReadMemory(this.currentHPBaseAddress);
        }

        public uint ReadCurrentSp()
        {
            return ReadMemory(this.currentHPBaseAddress + 8);
        }

        public uint ReadMaxHp()
        {
            return ReadMemory(this.currentHPBaseAddress + 4);
        }

        public string ReadCharacterName()
        {
            return ReadMemoryAsString(this.currentNameAddress);
        }

        public uint ReadMaxSp()
        {
            return ReadMemory(this.currentHPBaseAddress + 12);
        }

        public uint CurrentBuffStatusCode(int effectStatusIndex)
        {
            if (this.statusBufferAddress == 0)
            {
                return 0;
            }

            return ReadMemory(this.statusBufferAddress + effectStatusIndex * 4);
        }

        public Client GetClientByProcess(string procName)
        {
            foreach (Client c in ClientListSingleton.GetAll())
            {
                if (string.Equals(c.processName, procName, StringComparison.OrdinalIgnoreCase))
                {
                    uint hpBaseValue = ReadMemory(c.currentHPBaseAddress);
                    if (hpBaseValue > 0)
                    {
                        return c;
                    }
                }
            }

            return null;
        }

        public static Client FromDTO(ClientDTO dto)
        {
            if (dto == null)
            {
                return null;
            }

            int hpPtr = dto.hpAddressPointer != 0 ? dto.hpAddressPointer : Convert.ToInt32(dto.hpAddress, 16);
            int namePtr = dto.nameAddressPointer != 0 ? dto.nameAddressPointer : Convert.ToInt32(dto.nameAddress, 16);

            return ClientListSingleton.GetAll()
                .Where(c => string.Equals(c.processName, dto.name, StringComparison.OrdinalIgnoreCase))
                .Where(c => c.currentHPBaseAddress == hpPtr)
                .Where(c => c.currentNameAddress == namePtr)
                .FirstOrDefault();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;

            if (disposing)
            {
                if (this.PMR != null)
                {
                    this.PMR.Dispose();
                    this.PMR = null;
                }

                if (this.process != null)
                {
                    this.process.Dispose();
                    this.process = null;
                }
            }
        }
    }
}
