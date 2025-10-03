using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace NightreignRelicEditor.Models;

public class GameLink : IDisposable
{
    public const uint PROCESS_ALL_ACCESS = 0x1F0FFF;

    private string processName = "";
    private string moduleName = "";

    private Process gameProcess = null;
    private IntPtr gameProcessHandle = IntPtr.Zero;
    private IntPtr gameBaseAddress = IntPtr.Zero;
    private int gameSize = 0;
    private string gameVersion = "";

    private bool linkActive = false;
    private bool modulesFound = false;

    bool disposed = false;

    [DllImport("kernel32.dll")]
    private static extern IntPtr OpenProcess(uint dwDesiredAcess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int iSize, ref int lpNumberOfBytesRead);

    [DllImport("kernel32.dll")]
    private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, ref int lpNumberOfBytesWritten);

    [DllImport("ntdll.dll")]
    static extern int NtWriteVirtualMemory(IntPtr ProcessHandle, IntPtr BaseAddress, byte[] Buffer, UInt32 NumberOfBytesToWrite, ref UInt32 NumberOfBytesWritten);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    System.Windows.Threading.DispatcherTimer monitorTimer = new System.Windows.Threading.DispatcherTimer();

    public GameLink(string process, string module)
    {
        processName = process;
        moduleName = module;

        monitorTimer.Tick += monitorTimer_Tick;
        monitorTimer.Interval = TimeSpan.FromMilliseconds(1000);
            
    }

    public bool CheckProcessRunning(string processName)
    {
        var processList = Process.GetProcesses();

        foreach (var process in processList)
        {
            if (process.ProcessName.ToLower().Equals(processName))
                return true;
        }

        return false;
    }

    public void InitGameLink()
    {
        linkActive = AttachProcess();

        if (linkActive)
            modulesFound = LocateModules();

        if (modulesFound)
            monitorTimer.Start();
    }

    private bool AttachProcess()
    {
        var processList = Process.GetProcesses();

        foreach (var process in processList)
        {
            if (process.ProcessName.ToLower().Equals(processName) && !process.HasExited)
            {
                gameProcess = process;
                gameProcessHandle = OpenProcess(PROCESS_ALL_ACCESS, bInheritHandle: false, gameProcess.Id);
                return true;
            }
        }
        return false;
    }

    private bool LocateModules()
    {
        try
        {
            Debug.Print("Seeking module...");
            foreach (var module in gameProcess.Modules)
            {
                var processModule = module as ProcessModule;
                var currentModuleName = processModule.ModuleName.ToLower();

                if (currentModuleName == moduleName)
                {
                    gameBaseAddress = processModule.BaseAddress;
                    gameSize = processModule.ModuleMemorySize;
                    gameVersion = processModule.FileVersionInfo.FileVersion;
                    return true;
                }
            }
            Debug.Print("Module not found");
        }
        catch (Exception f) { MessageBox.Show("Locate module error - " + f.ToString()); }

        return false;
    }

    private void monitorTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            Process nightreignProcess = Process.GetProcessById(gameProcess.Id);
        }
        catch (Exception f)
        {
            linkActive = false;
            modulesFound = false;
            gameProcess = null;
            gameProcessHandle = IntPtr.Zero;
            gameBaseAddress = IntPtr.Zero;
            monitorTimer.Stop();
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
            return;

        if (disposing)
        {
            monitorTimer.Stop();

            if (gameProcessHandle != IntPtr.Zero)
            {
                try
                {
                    CloseHandle(gameProcessHandle);
                }
                catch (Exception f)
                {
                    Debug.Print(f.ToString());
                }
            }

            gameProcess = null;
            monitorTimer = null;
        }
        disposed = true;
    }

    public bool Connected
    {
        get
        {
            return linkActive && modulesFound;
        }
    }

    public IntPtr ProcessHandle
    {
        get
        {
            return gameProcessHandle;
        }
    }

    public IntPtr BaseAddress
    {
        get
        {
            return gameBaseAddress;
        }
    }

    public uint[] Version
    {
        get
        {
            string[] split = gameVersion.Split(".");

            if (split.Length == 0)
                return new uint[] { 0, 0, 0 };

            return new uint[3] { UInt32.Parse(split[0]), UInt32.Parse(split[1]), UInt32.Parse(split[2]) };
        }
    }

    public IntPtr ResolvePointerChain(params nint[] pointers)
    {
        IntPtr pointer = (IntPtr)ReadUInt64(pointers[0]);

        for (int i = 1; i < pointers.Length; i++)
        {
            pointer = (IntPtr)ReadUInt64(pointer + pointers[i]);
        }
        return pointer;
    }

    private byte[] ReadMemory(IntPtr address, int size)
    {
        var data = new byte[size];
        var i = 1;
        ReadProcessMemory(gameProcessHandle, address, data, size, ref i);
        return data;
    }

    public byte ReadByte(IntPtr address)
    {
        return ReadMemory(address, 1)[0];
    }

    public uint ReadUInt16(IntPtr address)
    {
        var data = ReadMemory(address, 2);
        return BitConverter.ToUInt16(data, 0);
    }

    public uint ReadUInt32(IntPtr address)
    {
        var data = ReadMemory(address, 4);
        return BitConverter.ToUInt32(data, 0);
    }

    public ulong ReadUInt64(IntPtr address)
    {
        var data = ReadMemory(address, 8);
        return BitConverter.ToUInt64(data, 0);
    }

    public int ReadInt32(IntPtr address)
    {
        var data = ReadMemory(address, 4);
        return BitConverter.ToInt32(data, 0);
    }

    public float ReadFloat(IntPtr address)
    {
        var bytes = ReadMemory(address, 4);
        return BitConverter.ToSingle(bytes, 0);
    }

    public double ReadDouble(IntPtr address)
    {
        var bytes = ReadMemory(address, 8);
        return BitConverter.ToDouble(bytes, 0);
    }

    public string ReadString(IntPtr address, int length)
    {
        if (length > 32) { length = 32; }
        if (length < 1) { length = 1; }
        return Encoding.Unicode.GetString(ReadMemory(address, length * 2));
    }

    public void WriteMemory(IntPtr address, byte[] data)
    {
        uint i = 0;
        NtWriteVirtualMemory(gameProcessHandle, address, data, (uint)data.Length, ref i);
    }

    public void WriteByte(IntPtr address, byte data)
    {
        var bytes = new byte[] { data };
        WriteMemory(address, bytes);
    }

    public void WriteUInt32(IntPtr address, uint data)
    {
        WriteMemory(address, BitConverter.GetBytes(data));
    }

    public void WriteFloat(IntPtr address, float data)
    {
        WriteMemory(address, BitConverter.GetBytes(data));
    }

    public void WriteDouble(IntPtr address, double data)
    {
        WriteMemory(address, BitConverter.GetBytes(data));
    }
}