using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace CF_Protocol
{
    struct CFLoginRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public char[] user;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        public char[] password;

        public CFLoginRequest(char[] u, char[] p)
        {
            user = new char[12];
            password = new char[18];
            Array.Copy(u, user, u.Length);
            Array.Copy(p, password, p.Length);
        }
    }
    struct CFSignupRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        char[] user;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        char[] password;
    }

    struct CFDeleteUserRequest
    {
    }
    struct CFDummySigninRequest
    {
    }
    struct CFUpdateUserRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        char[] password;

        public CFUpdateUserRequest(char[] p)
        {
            password = new char[18];
            Array.Copy(p, password, p.Length);
        }
    }
 

    public static class CFHelper
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

    struct CFBroadCast
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        char[] userName;
        //data[]
    }
    struct CFChat
    {
        //data[]
    }
    struct CFInitializeRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public char[] cookie;
    }

    //Login and ConnectPassing

    struct CFRoomRequest
    {
        //create, join, leave
        //none   
    }
    //create room response
    struct CFRoomResponse
    {
        int roomNo;
    }

    //Room list Response
    //list<int> list;

    struct CFSignupResponse
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        char[] cookie;
    }
    struct CFDeleteUserResponse
    {
    }
    struct CFUpdateUserResponse
    {
    }

    struct CFDummySigninResponse
    {
    }
    //Login and ConnectPassing
    struct CFSigninResponse
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        public char[] ip;

        public int port;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public char[] cookie;
    }

    struct CFSignoutResponse
    {
    }
    struct CFRoomCreateRequest
    {
    }

    struct CFRoomListRequest
    {
    }

    struct CFRoomJoinRequest
    {
        public int roomNum;
    }

    struct CFRoomLeaveRequest
    {
    }

    struct CFInitializeResponse
    {
    }
    struct CFRoomCreateResponse
    {
        int roomNum;
    }

    struct CFRoomListResponse
    {
        
    }

    struct CFRoomJoinResponse
    {
    }

    struct CFRoomLeaveResponse
    {
    }

    struct CFRoomJoinRedirectResponse
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        char[] ip;
        int port;
    }


}
