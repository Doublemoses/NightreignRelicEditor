using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Windows;

namespace NightreignRelicEditor.Models;

public class AOBScanner : IDisposable
{
    [DllImport("ntdll.dll")]
    static extern int NtReadVirtualMemory(IntPtr ProcessHandle, IntPtr BaseAddress, byte[] Buffer, UInt32 NumberOfBytesToRead, ref UInt32 NumberOfBytesRead);

    List<int> sectionAddress = new List<int>();
    List<int> sectionSize = new List<int>();
    List<byte[]> sectionData = new List<byte[]>();
    IntPtr moduleBaseAddress = IntPtr.Zero;

    bool disposed = false;

    public AOBScanner(IntPtr processHandle, IntPtr baseAddress, string sectionName)
    {
        byte[] buffer = new byte[0x600];
        uint bytesRead = 0;
        moduleBaseAddress = baseAddress;

        NtReadVirtualMemory(processHandle, baseAddress, buffer, 0x600, ref bytesRead);

        using (MemoryStream stream = new MemoryStream(buffer))
        using (PEReader reader = new PEReader(stream))
        {
            var headers = reader.PEHeaders;

            foreach (var section in headers.SectionHeaders)
            {
                if (section.Name == sectionName)
                {
                    sectionAddress.Add(section.VirtualAddress);
                    sectionSize.Add(section.SizeOfRawData);
                    sectionData.Add(new byte[section.SizeOfRawData]);
                    NtReadVirtualMemory(processHandle, baseAddress + section.VirtualAddress, sectionData[sectionData.Count - 1], (uint)section.SizeOfRawData, ref bytesRead);
                }
            }
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
            for (int x = 0; x < sectionData.Count; x++)
            {
                Array.Clear(sectionData[x]);
                sectionData[x] = null;
            }

            sectionAddress.Clear();
            sectionAddress = null;
            sectionSize.Clear();
            sectionSize = null;
            sectionData.Clear();
            sectionData = null;
        }
        disposed = true;
    }

    public IntPtr FindAddress(string patternSource, int startOffset = 0)
    {
        IntPtr returnValue = IntPtr.Zero;
        for (int x = 0; x <= sectionAddress.Count; x++)
        {
            returnValue = FindAddressInSection(patternSource, x, startOffset);

            if (returnValue != IntPtr.Zero)
                break;
        }
        return returnValue;
    }

    public IntPtr FindAddressInSection(string patternSource, int section = 0, int startOffset = 0)
    {
        if (section >= sectionAddress.Count)
            return IntPtr.Zero;

        (byte, bool)[] searchPattern = ConvertSearchStringToBytes(patternSource);
        int indexResult = ScanMemory(searchPattern, section, startOffset);

        if (indexResult != -1)
            return moduleBaseAddress + indexResult + sectionAddress[section];

        return IntPtr.Zero;
    }

    private (byte, bool)[] ConvertSearchStringToBytes(string patternSource)
    {
        string[] splitPattern = patternSource.Split(" ");
        (byte, bool)[] searchPattern = new (byte, bool)[splitPattern.Length];

        for (int i = 0; i < splitPattern.Length; i++)
        {
            if (splitPattern[i].Contains("?"))
            {
                searchPattern[i] = (0x0, true);
            }
            else
            {
                try
                {
                    searchPattern[i] = (Convert.ToByte(splitPattern[i], 16), false);
                }
                catch
                {
                    MessageBox.Show("you absolute dingus you typed the address in wrong");
                }
            }
        }
        return searchPattern;
    }

    private int ScanMemory((byte patternByte, bool isWildcard)[] searchPattern, int section, int startOffset)
    {
        for (int i = startOffset;
             i <= (sectionSize[section] - searchPattern.Length);
             i++)
        {
            if (searchPattern[0].patternByte == sectionData[section][i])
            {
                bool matching = true;
                int j = 1;

                while (matching)
                {
                    if (j >= searchPattern.Length)
                        return i;

                    if (!searchPattern[j].isWildcard && searchPattern[j].patternByte != sectionData[section][i + j])
                        matching = false;

                    j++;
                }
            }
        }
        return -1;
    }
}