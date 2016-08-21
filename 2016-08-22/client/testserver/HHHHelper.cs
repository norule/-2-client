﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using static Junhaehok.Packet;
using static System.BitConverter;

namespace Junhaehok
{
    public struct Packet
    {
        public struct Header
        {
            public long uid;
            public ushort code;
            public ushort size;
            public Header(ushort code, ushort size, long uid = 0)
            {
                this.uid = uid;
                this.code = code;
                this.size = size;
            }
        }

        public Header header;
        public byte[] data;

        public Packet(Header header, byte[] data)
        {
            this.header = header;
            this.data = data;
        }
    }

    public static class HhhHelper
    {
        public const int HEADER_SIZE = 12;
        public static byte[] PacketToBytes(Packet packet)
        {
            byte[] buffer = new byte[sizeof(long) + sizeof(ushort) + sizeof(ushort) + packet.data.Length];
            Array.Copy(GetBytes(packet.header.uid), 0, buffer, FieldIndex.UID, sizeof(long));
            Array.Copy(GetBytes(packet.header.code), 0, buffer, FieldIndex.CODE, sizeof(ushort));
            Array.Copy(GetBytes(packet.header.size), 0, buffer, FieldIndex.SIZE, sizeof(ushort));
            Array.Copy(packet.data, 0, buffer, FieldIndex.DATA, packet.data.Length);
            return buffer;
        }

        public static Packet BytesToPacket(byte[] bytes)
        {
            byte[] headerBytes = new byte[HEADER_SIZE];
            byte[] dataBytes = new byte[bytes.Length - HEADER_SIZE];
            Array.Copy(bytes, 0, headerBytes, 0, headerBytes.Length);
            Array.Copy(bytes, HEADER_SIZE, dataBytes, 0, dataBytes.Length);

            Header header = BytesToHeader(headerBytes);
            Packet packet = new Packet(header, dataBytes);

            return packet;
        }

        public static Header BytesToHeader(byte[] bytes)
        {
            Header header = new Header();

            header.uid = ToUInt16(bytes, FieldIndex.UID);
            header.code = ToUInt16(bytes, FieldIndex.CODE);
            header.size = ToUInt16(bytes, FieldIndex.SIZE);

            return header;
        }

        public class FieldIndex
        {
            public const int UID = 0;
            public const int CODE = 8;
            public const int SIZE = 10;
            public const int DATA = 12;
        }

        public class Code
        {
            public const ushort SIGNUP = 100;
            public const ushort SIGNUP_SUCCESS = 102;
            public const ushort SIGNUP_FAIL = 105;
            public const ushort DELETE_USER = 110;
            public const ushort DELETE_USER_SUCCESS = 112;
            public const ushort DELETE_USER_FAIL = 115;
            public const ushort UPDATE_USER = 120;
            public const ushort UPDATE_USER_SUCCESS = 122;
            public const ushort UPDATE_USER_USER_FAIL = 125;

            public const ushort SIGNIN = 200;
            public const ushort SIGNIN_SUCCESS = 202;
            public const ushort SIGNIN_FAIL = 205;
            public const ushort DUMMY_SIGNIN = 220;
            public const ushort DUMMY_SIGNIN_SUCCESS = 222;
            public const ushort DUMMY_SIGNIN_FAIL = 225;
            public const ushort INITIALIZE = 250; // CL to FE (check cookie as soon as connection established)
            public const ushort INITIALIZE_SUCCESS = 252;
            public const ushort INITIALIZE_FAIL = 255;

            public const ushort SIGNOUT = 300;
            public const ushort SIGNOUT_SUCCESS = 302;
            public const ushort SIGNOUT_FAIL = 305;

            public const ushort ROOM_LIST = 400;
            public const ushort ROOM_LIST_SUCCESS = 402;
            public const ushort ROOM_LIST_FAIL = 405;

            public const ushort CREATE_ROOM = 500;
            public const ushort CREATE_ROOM_SUCCESS = 502;
            public const ushort CREATE_ROOM_FAIL = 505;

            public const ushort JOIN = 600;
            public const ushort JOIN_SUCCESS = 602; //BE -> FE -> CL (room is in current FE - can join)
            public const ushort JOIN_FAIL = 605;
            public const ushort JOIN_FULL_FAIL = 615; //BE -> FE -> CL (room full)
            public const ushort JOIN_NULL_FAIL = 625; //BE -> FE -> CL (room does not exist)
            public const ushort JOIN_REDIRECT = 630;  //BE -> FE -> CL (room not in current FE - REDIRECT)

            public const ushort CONNECTION_PASS = 650; //BE -> FE (user is going your way with this cookie)
            public const ushort CONNECTION_PASS_SUCCESS = 652;
            public const ushort CONNECTION_PASS_FAIL = 655;

            public const ushort LEAVE_ROOM = 700;
            public const ushort LEAVE_ROOM_SUCCESS = 702;
            public const ushort LEAVE_ROOM_FAIL = 705;

            public const ushort DESTROY_ROOM = 800;
            public const ushort DESTROY_ROOM_SUCCESS = 802;
            public const ushort DESTROY_ROOM_FAIL = 805;

            public const ushort MSG = 900;
            public const ushort MSG_SUCCESS = 902;
            public const ushort MSG_FAIL = 905;

            public const ushort HEARTBEAT = 1000;
            public const ushort HEARTBEAT_SUCCESS = 1002;
            public const ushort HEARTBEAT_FAIL = 1005;
        }
        public static string PacketDebug(Packet p)
        {
            if (null == p.data)
                return "UID: " + p.header.uid + "\nCODE: " + p.header.code + "\nSIZE: " + p.header.size + "\nDATA: ";
            else
                return "UID: " + p.header.uid + "\nCODE: " + p.header.code + "\nSIZE: " + p.header.size + "\nDATA: " + Encoding.UTF8.GetString(p.data);
        }
    }
}