using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Admin
{
    public struct AAServerInfoResponse
    {
        public bool alive;
        public int roomCount;
        public int userCount;
    }

    public struct AADeleteUserRequest
    {
        public string ID;

        public AADeleteUserRequest(string input)
        {
            ID = input;
        }
    }
    public static class AAHelper
    { 

        public static byte[] StructureToByte(object obj)
        {
            int datasize = Marshal.SizeOf(obj);
            IntPtr buff = Marshal.AllocHGlobal(datasize);
            Marshal.StructureToPtr(obj, buff, false);
            byte[] data = new byte[datasize];
            Marshal.Copy(buff, data, 0, datasize);
            Marshal.FreeHGlobal(buff);
            return data;
        }
        public static object ByteToStructure(byte[] data, Type type)
        {
            IntPtr buff = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, buff, data.Length);
            object obj = Marshal.PtrToStructure(buff, type);
            Marshal.FreeHGlobal(buff);

            if (Marshal.SizeOf(obj) != data.Length)
            {
                return null;
            }
            return obj;
        }
    }
}
