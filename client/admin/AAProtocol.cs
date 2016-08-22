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
        public static  UserHandle[] ByteToRanking(byte[] data, Type type)
        {
            int objLength = data.Length / (Marshal.SizeOf(type));
            UserHandle[] objList = new UserHandle[objLength];

            for (int idx = 0; idx < objList.Length; idx++)
            {
                byte[] tmp = new byte[Marshal.SizeOf(type)];
                Array.Copy(data, Marshal.SizeOf(type) * idx, tmp, 0, tmp.Length);
                IntPtr buff = Marshal.AllocHGlobal(Marshal.SizeOf(type)); // 배열의 크기만큼 비관리 메모리 영역에 메모리를 할당한다.
                Marshal.Copy(tmp, 0, buff, tmp.Length); // 배열에 저장된 데이터를 위에서 할당한 메모리 영역에 복사한다.
                UserHandle obj = (UserHandle)Marshal.PtrToStructure(buff, type); // 복사된 데이터를 구조체 객체로 변환한다.
                Marshal.FreeHGlobal(buff); // 비관리 메모리 영역에 할당했던 메모리를 해제함
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
