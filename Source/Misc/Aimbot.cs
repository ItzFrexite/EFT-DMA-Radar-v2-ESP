using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Timers;
using Offsets;
using Vmmsharp;
using Vmmsharp.Internal;
using System.Security.Cryptography;
using OpenTK.Graphics.ES20;
using System.Diagnostics;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Reflection;
using static Vmmsharp.VmmProcess;

namespace eft_dma_radar
{

    public static class MemoryHandler
    {
        internal struct VMMDLL_MAP_EATENTRY
        {
            internal ulong vaFunction;

            internal uint dwOrdinal;

            internal uint oFunctionsArray;

            internal uint oNamesArray;

            internal uint _FutureUse1;

            [MarshalAs(UnmanagedType.LPUTF8Str)]
            internal string uszFunction;

            [MarshalAs(UnmanagedType.LPUTF8Str)]
            internal string uszForwardedFunction;
        }

        internal struct VMMDLL_MAP_EAT
        {
            internal uint dwVersion;

            internal uint dwOrdinalBase;

            internal uint cNumberOfNames;

            internal uint cNumberOfFunctions;

            internal uint cNumberOfForwardedFunctions;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            internal uint[] _Reserved1;

            internal ulong vaModuleBase;

            internal ulong vaAddressOfFunctions;

            internal ulong vaAddressOfNames;

            internal ulong pbMultiText;

            internal uint cbMultiText;

            internal uint cMap;
        }

        internal struct VMMDLL_MAP_MODULEENTRY
        {
            internal ulong vaBase;

            internal ulong vaEntry;

            internal uint cbImageSize;

            internal bool fWow64;

            [MarshalAs(UnmanagedType.LPUTF8Str)]
            internal string uszText;

            internal uint _Reserved3;

            internal uint _Reserved4;

            [MarshalAs(UnmanagedType.LPUTF8Str)]
            internal string uszFullName;

            internal uint tp;

            internal uint cbFileSizeRaw;

            internal uint cSection;

            internal uint cEAT;

            internal uint cIAT;

            internal uint _Reserved2;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            internal ulong[] _Reserved1;

            internal nint pExDebugInfo;

            internal nint pExVersionInfo;
        }

        [DllImport("vmm", EntryPoint = "VMMDLL_MemReadEx", ExactSpelling = true)]
        public static extern bool VMMDLL_MemReadEx(
        IntPtr hVMM,
        uint dwPID,
        ulong qwA,
        byte[] pb,
        uint cb,
        out uint pcbReadOpt,
        uint flags
    );

        [DllImport("vmm", EntryPoint = "VMMDLL_PdbSymbolAddress", CharSet = CharSet.Ansi)]
        private static extern bool VMMDLL_PdbSymbolAddress(
            IntPtr hVMM,
            [MarshalAs(UnmanagedType.LPStr)] string szModule,
            [MarshalAs(UnmanagedType.LPStr)] string szSymbolName,
            out ulong pvaSymbolAddress
    );

        [DllImport("vmm", EntryPoint = "VMMDLL_Map_GetModuleFromNameW", ExactSpelling = true)]
        public unsafe static extern bool VMMDLL_Map_GetModuleFromNameW(
            IntPtr hVMM,
            uint dwPID,
            [MarshalAs(UnmanagedType.LPWStr)] string uszModuleName,
            out nint ppModuleMapEntry,
            uint flags
    );

        [DllImport("vmm", EntryPoint = "VMMDLL_PdbLoad", CharSet = CharSet.Ansi)]
        private static extern bool VMMDLL_PdbLoad(
            IntPtr hVMM,
            uint dwPID,
            ulong vaModuleBase,
            [Out] StringBuilder szModuleName
        );


        [DllImport("vmm", EntryPoint = "VMMDLL_Map_GetEATU", CharSet = CharSet.Unicode)]
        public unsafe static extern bool VMMDLL_Map_GetEATU(
            IntPtr hVMM,
            uint dwPid,
            [MarshalAs(UnmanagedType.LPStr)] string uszModuleName,
            out IntPtr ppEatMap
    );

        [DllImport("vmm", EntryPoint = "VMMDLL_MemFree", ExactSpelling = true)]
        public unsafe static extern void VMMDLL_MemFree(byte* pvMem);

        public static bool GetPdbSymbolAddress(IntPtr hVMM, string moduleName, string symbolName, out ulong symbolAddress)
        {
            return VMMDLL_PdbSymbolAddress(hVMM, moduleName, symbolName, out symbolAddress);
        }

        public static bool PdbLoad(IntPtr hVMM, uint pid, ulong moduleBase, out string moduleName)
        {
            StringBuilder buffer = new StringBuilder(32);
            bool result = VMMDLL_PdbLoad(hVMM, pid, moduleBase, buffer);
            moduleName = result ? buffer.ToString() : null;
            return result;
        }

        public unsafe static bool GetExportFr(Vmm vmm_handle, uint process_pid, string module_name, string export_name, out ulong fnc_addy)
        {
            fnc_addy = 0;

            nint pipi = IntPtr.Zero;
            EATEntry[] array = new EATEntry[0];
            int num = Marshal.SizeOf<VMMDLL_MAP_EAT>();
            int num2 = Marshal.SizeOf<VMMDLL_MAP_EATENTRY>();

            bool success = MemoryHandler.VMMDLL_Map_GetEATU(vmm_handle, process_pid, module_name, out pipi);
            if (!success)
            {
                Program.Log("Failed to get EAT");
                return false;
            }

            VMMDLL_MAP_EAT eat = Marshal.PtrToStructure<VMMDLL_MAP_EAT>(pipi);
            if (eat.dwVersion != 3)
            {
                Program.Log("Invalid dwVersion");
                MemoryHandler.VMMDLL_MemFree((byte*)((IntPtr)pipi).ToPointer());
                return false;
            }
            Program.Log($"Number of functions: {eat.cNumberOfFunctions.ToString()}");
            Program.Log($"Cmap Count: {eat.cMap.ToString()}");

            array = new EATEntry[eat.cMap];
            for (int i = 0; i < eat.cMap; i++)
            {
                VMMDLL_MAP_EATENTRY eatentry = Marshal.PtrToStructure<VMMDLL_MAP_EATENTRY>((nint)(((IntPtr)pipi).ToInt64() + num + i * num2));
                if (string.Equals(eatentry.uszFunction, export_name))
                {
                    Program.Log("Found function VA");
                    fnc_addy = eatentry.vaFunction;
                    return true;
                }
                Program.Log(eatentry.uszFunction);
            }

            Program.Log("Failed to find exported function");
            MemoryHandler.VMMDLL_MemFree((byte*)((IntPtr)pipi).ToPointer());
            return false;
        }

    }


    [StructLayout(LayoutKind.Sequential)]
    public struct Matrix
    {
        public float M11, M12, M13, M14;
        public float M21, M22, M23, M24;
        public float M31, M32, M33, M34;
        public float M41, M42, M43, M44;

        public static Matrix Identity => new Matrix
        {
            M11 = 1f,
            M12 = 0f,
            M13 = 0f,
            M14 = 0f,
            M21 = 0f,
            M22 = 1f,
            M23 = 0f,
            M24 = 0f,
            M31 = 0f,
            M32 = 0f,
            M33 = 1f,
            M34 = 0f,
            M41 = 0f,
            M42 = 0f,
            M43 = 0f,
            M44 = 1f
        };

        public Matrix(
            float m11, float m12, float m13, float m14,
            float m21, float m22, float m23, float m24,
            float m31, float m32, float m33, float m34,
            float m41, float m42, float m43, float m44)
        {
            M11 = m11; M12 = m12; M13 = m13; M14 = m14;
            M21 = m21; M22 = m22; M23 = m23; M24 = m24;
            M31 = m31; M32 = m32; M33 = m33; M34 = m34;
            M41 = m41; M42 = m42; M43 = m43; M44 = m44;
        }

        public static Matrix operator *(Matrix left, Matrix right)
        {
            Matrix result = new Matrix();
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    float sum = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        sum += left[row, i] * right[i, col];
                    }
                    result[row, col] = sum;
                }
            }
            return result;
        }

        public float this[int row, int column]
        {
            get
            {
                return row switch
                {
                    0 => column switch { 0 => M11, 1 => M12, 2 => M13, 3 => M14, _ => throw new ArgumentOutOfRangeException(nameof(column)) },
                    1 => column switch { 0 => M21, 1 => M22, 2 => M23, 3 => M24, _ => throw new ArgumentOutOfRangeException(nameof(column)) },
                    2 => column switch { 0 => M31, 1 => M32, 2 => M33, 3 => M34, _ => throw new ArgumentOutOfRangeException(nameof(column)) },
                    3 => column switch { 0 => M41, 1 => M42, 2 => M43, 3 => M44, _ => throw new ArgumentOutOfRangeException(nameof(column)) },
                    _ => throw new ArgumentOutOfRangeException(nameof(row))
                };
            }
            set
            {
                switch (row)
                {
                    case 0:
                        switch (column)
                        {
                            case 0: M11 = value; break;
                            case 1: M12 = value; break;
                            case 2: M13 = value; break;
                            case 3: M14 = value; break;
                            default: throw new ArgumentOutOfRangeException(nameof(column));
                        }
                        break;
                    case 1:
                        switch (column)
                        {
                            case 0: M21 = value; break;
                            case 1: M22 = value; break;
                            case 2: M23 = value; break;
                            case 3: M24 = value; break;
                            default: throw new ArgumentOutOfRangeException(nameof(column));
                        }
                        break;
                    case 2:
                        switch (column)
                        {
                            case 0: M31 = value; break;
                            case 1: M32 = value; break;
                            case 2: M33 = value; break;
                            case 3: M34 = value; break;
                            default: throw new ArgumentOutOfRangeException(nameof(column));
                        }
                        break;
                    case 3:
                        switch (column)
                        {
                            case 0: M41 = value; break;
                            case 1: M42 = value; break;
                            case 2: M43 = value; break;
                            case 3: M44 = value; break;
                            default: throw new ArgumentOutOfRangeException(nameof(column));
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(row));
                }
            }
        }

        public static Matrix Transpose(Matrix pM)
        {
            Matrix pOut = new Matrix();
            pOut.M11 = pM.M11;
            pOut.M12 = pM.M21;
            pOut.M13 = pM.M31;
            pOut.M14 = pM.M41;
            pOut.M21 = pM.M12;
            pOut.M22 = pM.M22;
            pOut.M23 = pM.M32;
            pOut.M24 = pM.M42;
            pOut.M31 = pM.M13;
            pOut.M32 = pM.M23;
            pOut.M33 = pM.M33;
            pOut.M34 = pM.M43;
            pOut.M41 = pM.M14;
            pOut.M42 = pM.M24;
            pOut.M43 = pM.M34;
            pOut.M44 = pM.M44;
            return pOut;
        }
    }



    public class InputHandler //Credits to metick's DMA c++ library
    {
        public static bool _doneInitialization = false;
        private static int try_count = 0;
        VmmProcess winlogon;
        private uint win_logon_pid;
        private ulong gafAsyncKeyStateExport;
        private byte[] state_bitmap = new byte[64];
        private byte[] previous_state_bitmap = new byte[256 / 8];
        private Stopwatch stopwatch = Stopwatch.StartNew();

        private Vmm mem
        {
            get => Memory.VMM;
        }

        public bool Init()
        {
            if (_doneInitialization) return true;

            if (try_count > 3)
            {
                _doneInitialization = true;
                Program.Log("Failed to initialize input handler in 3+ attempts");
                return false;
            }

            var meow = mem.RegValueRead("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\CurrentBuild", out _);
            var Winver = Int32.Parse(System.Text.Encoding.Unicode.GetString(meow));


            var mrrp = mem.RegValueRead("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\UBR", out _);
            uint Ubr = BitConverter.ToUInt32(mrrp);


            this.winlogon = mem.Process("winlogon.exe");
            this.win_logon_pid = winlogon.PID;

            if (winlogon.PID == 0)
            {
                Program.Log("Winlogon not found");
                try_count += 1;
                return false;
            }

            if (Winver > 22000)
            {
                Program.Log("Winver greater than 2200, attempting to read with offset");

                List<VmmProcess> crsissies = new List<VmmProcess>();

                foreach (var proc in mem.Processes)
                {
                    var info = proc.GetInfo();
                    if (info.sName == "csrss.exe" || info.sNameLong == "csrss.exe")
                    {
                        crsissies.Add(proc);
                    }
                }

                Program.Log($"Found: {crsissies.Count()} crsissies");

                foreach (var csrs in crsissies)
                {
                    ulong temp = csrs.GetModuleBase("win32ksgd.sys");
                    if (temp == 0) continue;
                    ulong g_session_global_slots = temp + 0x3110;

                    ulong? t1 = csrs.MemReadAs<ulong>(g_session_global_slots);
                    ulong? t2 = csrs.MemReadAs<ulong>(t1.Value);
                    ulong? t3 = csrs.MemReadAs<ulong>(t2.Value);
                    ulong user_session_state = t3.Value;


                    if (Winver >= 22631 && Ubr >= 3810)
                    {
                        Program.Log("Win11 detected");
                        this.gafAsyncKeyStateExport = user_session_state + 0x36A8;
                    }
                    else
                    {
                        Program.Log("Older windows version detected, Attempting to resolve by offset");
                        this.gafAsyncKeyStateExport = user_session_state + 0x3690;
                    }
                    if (gafAsyncKeyStateExport > 0x7FFFFFFFFFFF)
                        break;
                }
                if (gafAsyncKeyStateExport > 0x7FFFFFFFFFFF)
                {
                    Program.Log("InputHandler success");
                    _doneInitialization = true;
                    return true;
                }
            }
            else
            {
                Program.Log("Older winver detected, attempting to resolve via EAT");
                ulong kitty = 0;
                bool success = MemoryHandler.GetExportFr(mem, win_logon_pid | Vmm.PID_PROCESS_WITH_KERNELMEMORY, "win32kbase.sys", "gafAsyncKeyState", out kitty);

                if (success)
                {
                    if (kitty >= 0x7FFFFFFFFFFF)
                    {
                        Program.Log("Resolved export via getexport");
                        this.gafAsyncKeyStateExport = kitty;
                        _doneInitialization = true;
                        return true;
                    }
                }

                Program.Log("Failed to resolve via EAT, attempting to resolve with PDB");
                nint moduleinfo = IntPtr.Zero;
                if (MemoryHandler.VMMDLL_Map_GetModuleFromNameW(mem, win_logon_pid | Vmm.PID_PROCESS_WITH_KERNELMEMORY, "win32kbase.sys", out moduleinfo, 0))
                {
                    MemoryHandler.VMMDLL_MAP_EAT mod = Marshal.PtrToStructure<MemoryHandler.VMMDLL_MAP_EAT>(moduleinfo);

                    string name;
                    if (MemoryHandler.PdbLoad(mem, win_logon_pid | Vmm.PID_PROCESS_WITH_KERNELMEMORY, mod.vaModuleBase, out name))
                    {
                        Program.Log("Downloaded pdb");
                        ulong gafgaf = 0;
                        if (MemoryHandler.GetPdbSymbolAddress(mem, name, "gafAsyncKeyState", out gafgaf))
                        {
                            Program.Log("Found PDB Symbol address");
                            if (gafgaf >= 0x7FFFFFFFFFFF)
                            {
                                Program.Log("Resolved export via pdb");
                                this.gafAsyncKeyStateExport = gafgaf;
                                _doneInitialization = true;
                                return true;
                            }
                        }
                    }
                }

            }
            Program.Log("Failed to find export");
            try_count += 1;
            return false;
        }

        public void UpdateKeys()
        {
            byte[] previous_key_state_bitmap = new byte[64];
            Array.Copy(state_bitmap, previous_key_state_bitmap, 64);

            bool success = MemoryHandler.VMMDLL_MemReadEx(mem, win_logon_pid | Vmm.PID_PROCESS_WITH_KERNELMEMORY, gafAsyncKeyStateExport, state_bitmap, 64, out _, Vmm.FLAG_NOCACHE);

            if (!success)
            {
                Program.Log("Update Keys Fail");
                return;
            }

            for (int vk = 0; vk < 256; ++vk)
            {
                if ((state_bitmap[(vk * 2 / 8)] & 1 << vk % 4 * 2) != 0 && (previous_key_state_bitmap[(vk * 2 / 8)] & 1 << vk % 4 * 2) == 0)
                {
                    previous_state_bitmap[vk / 8] |= (byte)(1 << vk % 8);
                }
            }
        }

        public bool IsKeyDown(Int32 virtual_key_code)
        {
            if (gafAsyncKeyStateExport < 0x7FFFFFFFFFFF)
                return false;
            if (stopwatch.ElapsedMilliseconds > 1)
            {
                UpdateKeys();
                stopwatch.Restart();
            }
            return (state_bitmap[(virtual_key_code * 2 / 8)] & 1 << virtual_key_code % 4 * 2) != 0;
        }
    }
    public class Aimbot
    {
        private Config _config;  // Declare _config

        private float _aimbotFOV;        // Field of View
        private float _aimbotMaxDistance; // Max Distance
        private int _aimbotKeybind;      // Keybind
        private float screenWidth;
        private float screenHeight;

        public Aimbot()
        {
            _config = Program.Config;  // Initialize _config from Program.Config
        }
        private Player _player;
        bool bLastHeld;
        private static InputHandler keyboard = new InputHandler();

        public KeyChecker _keyChecker = new KeyChecker();
        public static float Rad2Deg(float rad)
        {
            return rad * (180.0f / (float)Math.PI);
        }
        private static void NormalizeAngle(ref Vector2 angle)
        {
            var newX = angle.X switch
            {
                <= -180f => angle.X + 360f,
                > 180f => angle.X - 360f,
                _ => angle.X
            };

            var newY = angle.Y switch
            {
                > 90f => angle.Y - 180f,
                <= -90f => angle.Y + 180f,
                _ => angle.Y
            };

            angle = new Vector2(newX, newY);
        }

        public static Vector2 CalcAngle(Vector3 source, Vector3 destination)
        {
            Vector3 difference = source - destination;
            float length = difference.Length();
            Vector2 ret = new Vector2();

            ret.Y = (float)Math.Asin(difference.Y / length);
            ret.X = -(float)Math.Atan2(difference.X, -difference.Z);
            ret = new Vector2(ret.X * 57.29578f, ret.Y * 57.29578f);

            return ret;
        }


        public class KeyChecker
        {
            private const int MaxCallsPerSecond = 15;
            private const int Interval = 1000 / MaxCallsPerSecond;
            private bool _bHeld;
            private System.Timers.Timer _timer;

            public KeyChecker()
            {
                _timer = new System.Timers.Timer(Interval);
                _timer.Elapsed += CheckKey;
                _timer.AutoReset = true;
            }

            private void CheckKey(object sender, ElapsedEventArgs e)
            {
                _bHeld = KmBoxWrapper.IsKeyDown(0x1339);
            }

            public bool GetHeldState()
            {
                return _bHeld;
            }

            public void Start()
            {
                _timer.Start();
            }

            public void Stop()
            {
                _timer.Stop();
            }
        }


        public class KmBoxWrapper // Create a wrapper class (optional, but recommended)
        {
            public static bool done_init = false;

            [DllImport("KmerBoxer.dll", EntryPoint = "KmInit")]
            public static extern bool Init();

            [DllImport("KmerBoxer.dll", EntryPoint = "KmMove")]
            public static extern void Move(int x, int y);

            [DllImport("KmerBoxer.dll", EntryPoint = "KmIsKeyDown")]
            public static extern bool IsKeyDown(int keycode);

            [DllImport("KmerBoxer.dll", EntryPoint = "KmClick")]
            public static extern void LeftClick();
        }

        private CameraManager _cameraManager
        {
            get => Memory.CameraManager;
        }
        private ReadOnlyDictionary<string, Player> AllPlayers
        {
            get => Memory.Players;
        }
        private bool InGame
        {
            get => Memory.InGame;
        }

        private PlayerManager playamanaga
        {
            get => Memory.PlayerManager;
        }

        private Player LocalPlayer
        {
            get => Memory.LocalPlayer;
        }
        public Vector3 GetFireportPos()
        {
            if (!this.InGame || Memory.InHideout)
            {
                MessageBox.Show("Not in game");
                return new Vector3();
            }
            ulong handscontainer = Memory.ReadPtrChain(playamanaga._proceduralWeaponAnimation, new uint[] { ProceduralWeaponAnimation.FirearmContoller, FirearmController.Fireport, Fireport.To_TransfromInternal[0], Fireport.To_TransfromInternal[1] });
            Transform tranny = new Transform(handscontainer);
            Vector3 goofy = tranny.GetPosition();
            return new Vector3(goofy.X, goofy.Z, goofy.Y);
        }

        private float D3DXVec3Dot(Vector3 a, Vector3 b)
        {
            return (a.X * b.X +
                    a.Y * b.Y +
                    a.Z * b.Z);
        }

        private bool WorldToScreen(Vector3 _Enemy, out Vector2 _Screen)
        {
            _Screen = new Vector2(0, 0);

            Matrix viewMatrix = _cameraManager.ViewMatrix;
            Matrix temp = Matrix.Transpose(viewMatrix);

            Vector3 translationVector = new Vector3(temp.M41, temp.M42, temp.M43);
            Vector3 up = new Vector3(temp.M21, temp.M22, temp.M23);
            Vector3 right = new Vector3(temp.M11, temp.M12, temp.M13);

            float w = D3DXVec3Dot(translationVector, _Enemy) + temp.M44;

            if (w < 0.098f)
            {
                return false;
            }

            // Calculate screen coordinates
            float y = D3DXVec3Dot(up, _Enemy) + temp.M24;
            float x = D3DXVec3Dot(right, _Enemy) + temp.M14;

            _Screen.X = (screenWidth / 2f) * (1f + x / w);
            _Screen.Y = (screenHeight / 2f) * (1f - y / w);

            return true;
        }

        public bool GetHeadScr(Player player, out Vector2 screen, out Vector3 pos)
        {
            screen = new Vector2();
            pos = new Vector3();
            if (player.BoneTransforms != null && player.BoneTransforms.Count != 0 && !player.IsLocalPlayer && !player.IsFriendlyActive && player.IsAlive && player.IsActive && Vector3.Distance(player.Position, LocalPlayer.Position) < 100)
            {
                Vector3 HeadPos = new Vector3(player.HeadPosition.X, player.HeadPosition.Z, player.HeadPosition.Y);
                if (WorldToScreen(HeadPos, out Vector2 scrpos))
                {
                    screen = scrpos;
                    pos = HeadPos;
                    return true;
                }
            }
            return false;
        }

        public bool GetBoneScr(Player player, PlayerBones bone, out Vector2 screen, out Vector3 pos)
        {
            screen = new Vector2();
            pos = new Vector3();

            if (player.boneTransforms != null && player.boneTransforms.Count > 0 && !player.IsLocalPlayer && !player.IsFriendlyActive && player.IsAlive && player.IsActive && Vector3.Distance(player.Position, LocalPlayer.Position) < _config.AimbotMaxDistance)
            {
                Vector3 BonePos;

                switch (bone)
                {
                    case PlayerBones.HumanHead:
                        BonePos = new Vector3(player.HeadPosition.X, player.HeadPosition.Z, player.HeadPosition.Y);
                        break;
                    case PlayerBones.HumanNeck:
                        BonePos = new Vector3(player.HeadPosition.X, player.HeadPosition.Z, player.HeadPosition.Y);
                        break;
                    case PlayerBones.HumanSpine3:
                        BonePos = new Vector3(player.Spine3Position.X, player.Spine3Position.Z, player.Spine3Position.Y);
                        break;
                    case PlayerBones.HumanPelvis:
                        BonePos = new Vector3(player.PelvisPosition.X, player.PelvisPosition.Z, player.PelvisPosition.Y);
                        break;
                    case PlayerBones.HumanRCalf:
                        BonePos = new Vector3(player.RCalfPosition.X, player.RCalfPosition.Z, player.RCalfPosition.Y);
                        break;
                    case PlayerBones.HumanLCalf:
                        BonePos = new Vector3(player.LCalfPosition.X, player.LCalfPosition.Z, player.LCalfPosition.Y);
                        break;
                    default:
                        return false; // Bone not found or handled
                }

                if (WorldToScreen(BonePos, out Vector2 scrpos))
                {
                    screen = scrpos;
                    pos = BonePos;
                    return true;
                }
            }

            return false;
        }


        // Updated AimerBotter to use GetBoneScr for targeting multiple bones
        public void AimerBotter()
        {
            _aimbotFOV = _config.AimbotFOV;
            _aimbotMaxDistance = _config.AimbotMaxDistance; // Max targeting distance
            _aimbotKeybind = _config.AimbotKeybind;
            bool aimbotClosest = _config.AimbotClosest;
            screenWidth = Screen.PrimaryScreen.Bounds.Width;
            screenHeight = Screen.PrimaryScreen.Bounds.Height;

            if (!InputHandler._doneInitialization)
            {
                if (keyboard.Init())
                    Program.Log("Keyboard hook initialized");
            }

            // Check if the aimbot key is held down
            bool bHeld = keyboard.IsKeyDown(_aimbotKeybind);

            try
            {
                if (this.InGame && !Memory.InHideout && _cameraManager != null)
                {
                    // Filter active and alive players within max distance
                    var players = this.AllPlayers?.Select(x => x.Value)
                        .Where(x => x.IsActive && x.IsAlive && Vector3.Distance(x.Position, LocalPlayer.Position) < _config.AimbotMaxDistance)
                        .ToList();

                    if (players != null && players.Any())
                    {
                        this._cameraManager.GetViewmatrixAsync();
                        Vector3 cameraPos = GetFireportPos();

                        if (bHeld && bHeld == bLastHeld && _player != null && _player.IsAlive && _player.IsActive)
                        {
                            // Existing target lock logic
                            Vector3 targetPos = GetClosestBoneScr(_player, out Vector2 screenPos);
                            Vector2 rel = new Vector2(screenPos.X - (screenWidth / 2f), screenPos.Y - (screenHeight / 2f));
                            var distToCrosshair = Math.Sqrt((rel.X * rel.X) + (rel.Y * rel.Y));

                            if (distToCrosshair < _aimbotFOV)
                            {
                                Vector2 ang = CalcAngle(cameraPos, targetPos);
                                if (!float.IsNaN(ang.X) && !float.IsNaN(ang.Y))
                                {
                                    LocalPlayer.SetRotationFr(ang);
                                }
                            }
                        }
                        else if (bHeld && (bHeld != bLastHeld || _player == null || !_player.IsAlive || !_player.IsActive))
                        {
                            // Start searching for valid targets within FOV
                            List<Player> validTargets = new List<Player>();

                            foreach (var player in players)
                            {
                                Vector3 targetPos = GetClosestBoneScr(player, out Vector2 screenPos);
                                Vector2 rel = new Vector2(screenPos.X - (screenWidth / 2f), screenPos.Y - (screenHeight / 2f));
                                var distToCrosshair = Math.Sqrt((rel.X * rel.X) + (rel.Y * rel.Y));

                                // Only consider players within FOV
                                if (distToCrosshair < _aimbotFOV && distToCrosshair > 2)
                                {
                                    validTargets.Add(player);
                                }
                            }

                            if (validTargets.Count > 1 && aimbotClosest)
                            {
                                // Multiple targets in FOV, prioritize the one closest to the player
                                Player closestPlayer = validTargets
                                    .OrderBy(player => Vector3.Distance(player.Position, LocalPlayer.Position))
                                    .FirstOrDefault();

                                if (closestPlayer != null)
                                {
                                    Vector3 closestBone = GetClosestBoneScr(closestPlayer, out Vector2 screenPos);
                                    Vector2 ang = CalcAngle(cameraPos, closestBone);

                                    if (!float.IsNaN(ang.X) && !float.IsNaN(ang.Y))
                                    {
                                        LocalPlayer.SetRotationFr(ang);
                                        _player = closestPlayer;  // Lock onto closest target
                                    }
                                }
                            }
                            else if (validTargets.Count > 0)
                            {
                                // If only one target or AimbotClosest is off, target the closest one to the crosshair
                                Player closestToCrosshair = validTargets
                                    .OrderBy(player =>
                                    {
                                        GetClosestBoneScr(player, out Vector2 screenPos);
                                        return Vector2.Distance(screenPos, new Vector2(screenWidth / 2f, screenHeight / 2f));
                                    })
                                    .FirstOrDefault();

                                if (closestToCrosshair != null)
                                {
                                    Vector3 closestBone = GetClosestBoneScr(closestToCrosshair, out Vector2 screenPos);
                                    Vector2 ang = CalcAngle(cameraPos, closestBone);

                                    if (!float.IsNaN(ang.X) && !float.IsNaN(ang.Y))
                                    {
                                        LocalPlayer.SetRotationFr(ang);
                                        _player = closestToCrosshair;  // Lock onto the closest to the crosshair
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Log($"ERROR -> Aimer botter -> {ex.Message}\nStackTrace:{ex.StackTrace}");
            }

            bLastHeld = bHeld;  // Update the held state for the next frame
        }


        public Vector3 GetClosestBoneScr(Player player, out Vector2 screenPos)
        {
            Vector3 closestBonePos = new Vector3();
            screenPos = new Vector2();
            double closestDistance = double.MaxValue;

            List<(bool, PlayerBones)> boneOptions = new List<(bool, PlayerBones)>
            {
                (_config.AimbotHead, PlayerBones.HumanHead),
                (_config.AimbotNeck, PlayerBones.HumanNeck),
                (_config.AimbotChest, PlayerBones.HumanSpine3),
                (_config.AimbotPelvis, PlayerBones.HumanPelvis),
                (_config.AimbotRightLeg, PlayerBones.HumanRCalf),
                (_config.AimbotLeftLeg, PlayerBones.HumanLCalf)
            };

            foreach (var (isEnabled, bone) in boneOptions)
            {
                if (!isEnabled) continue;

                if (GetBoneScr(player, bone, out Vector2 boneScreenPos, out Vector3 bonePos))
                {
                    float distanceToCenter = Vector2.Distance(boneScreenPos, new Vector2(screenWidth / 2f, screenHeight / 2f));

                    if (distanceToCenter < closestDistance)
                    {
                        closestDistance = distanceToCenter;
                        closestBonePos = bonePos;
                        screenPos = boneScreenPos;
                    }
                }
            }

            return closestBonePos;
        }
        public void AimerBotterKmBox()
        {
            if (!KmBoxWrapper.done_init)
            {
                KmBoxWrapper.Init();
                KmBoxWrapper.done_init = true;
                _keyChecker.Start();
                Program.Log("Initialized kmbox");
            }

            if (_cameraManager is null)
            {
                Program.Log("Camera is null");
                return;
            }

            this._cameraManager.GetViewmatrixAsync();


            if (!KmBoxWrapper.done_init)
            {
                KmBoxWrapper.Init();
                KmBoxWrapper.done_init = true;
                _keyChecker.Start();
                Program.Log("Initialized kmbox");
            }

            try
            {
                if (!this.InGame || Memory.InHideout)
                {
                    MessageBox.Show("Not in game");
                    return;
                }




                bool bHeld = _keyChecker.GetHeldState();
                if (bHeld && bHeld == bLastHeld && _player is not null && _player.IsAlive && _player.IsActive)
                {
                    GetHeadScr(_player, out Vector2 headPos, out _);
                    Vector2 rel = new Vector2(headPos.X - (screenWidth / 2f), headPos.Y - (screenHeight / 2f));
                    var dist = Math.Sqrt(Math.Abs(rel.X) * Math.Abs(rel.Y));

                    if (dist < 90)
                    {
                        KmBoxWrapper.Move(Convert.ToInt32(rel.X), Convert.ToInt32(rel.Y));
                    }

                }
                else if (bHeld && (bHeld != bLastHeld || _player is null || !_player.IsAlive || !_player.IsActive))
                {
                    var players = this.AllPlayers?
                    .Select(x => x.Value)
                    .Where(x => x.IsActive && x.IsAlive);
                    if (players is not null)
                    {

                        Player clozestPlayer = null;
                        Vector2 clozestPlayerHead = Vector2.Zero;
                        double lastDist = 999999;
                        foreach (var player in players)
                        {
                            GetHeadScr(player, out Vector2 HeadPos, out _);
                            Vector2 rel = new Vector2(HeadPos.X - (screenWidth / 2f), HeadPos.Y - (screenHeight / 2f));

                            var dist = Math.Sqrt(Math.Abs(rel.X + 1) * Math.Abs(rel.Y + 1));
                            if (dist < lastDist && dist > 2)
                            {
                                clozestPlayer = player;
                                clozestPlayerHead = rel;
                                lastDist = dist;
                            }
                        }
                        if (lastDist < 90)
                        {
                            KmBoxWrapper.Move(Convert.ToInt32(clozestPlayerHead.X), Convert.ToInt32(clozestPlayerHead.Y));
                            _player = clozestPlayer;
                        }
                    }
                }
                bLastHeld = bHeld;
            }
            catch (Exception ex)
            {
                Program.Log($"ERROR -> Aimer botter -> {ex.Message}\nStackTrace:{ex.StackTrace}");
            }
        }
    }
}