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
        public static  UserHandle[] ByteToRanking(byte[] data, Type type)       //byto array to UserHandle
        {
            int objLength = data.Length / (Marshal.SizeOf(type));
            UserHandle[] objList = new UserHandle[objLength];

            for (int idx = 0; idx < objList.Length; idx++)
            {
                byte[] tmp = new byte[Marshal.SizeOf(type)];
                Array.Copy(data, Marshal.SizeOf(type) * idx, tmp, 0, tmp.Length);
                IntPtr buff = Marshal.AllocHGlobal(Marshal.SizeOf(type));
                Marshal.Copy(tmp, 0, buff, tmp.Length); 
                UserHandle obj = (UserHandle)Marshal.PtrToStructure(buff, type); 
                Marshal.FreeHGlobal(buff);
                objList[idx] = obj;
            }
            return objList;
        }

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

    public struct UserHandle
    {
        public int Rank;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public char[] ID;
        public int MSGCOUNT;
        public UserHandle(int r, char[] i, int mc)
        {
            Rank = r;
            ID = new char[12];
            Array.Copy(i, ID, i.Length);
            MSGCOUNT = mc;
        }
    }
}
