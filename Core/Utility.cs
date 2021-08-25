extern alias sdk;

using sdk::Microsoft.Xna.Framework.Graphics;
using sdk::osu;
using sdk::osu.GameModes.Play;
using sdk::osu.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public static class Utility
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string lpFileName);
        [DllImport("kernel32.dll")]
        public static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize,
            int flNewProtect, out int lpflOldProtect);
        [DllImport("kernel32.dll")]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        private const int RNDM_FCTR = 4;
        private static Random random = new Random();
        public static double Random(double min, double max)
        {
            double ret = min;
            for (int i = 0; i < RNDM_FCTR; i++)
                ret += random.NextDouble() * (max - min) / RNDM_FCTR;
            return ret;
        }
        public static string RandomHex(int cnt)
        {
            string s = "";
            for (int i = 0; i < cnt; i++)
                s += random.Next(0, 255).ToString("x2");
            return s;
        }

        public static bool CanPlay() => GameBase.Instance != null && Player.Instance != null
                && Player.Loaded && GameBase.Mode == OsuModes.Play && !InputManager.get_ReplayMode()
                && !Player.Paused;
        
        public static Color HSV(double hue, double saturation, double value)
        {
            hue *= 360;
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return new Color((byte)v, (byte)t, (byte)p);
            else if (hi == 1)
                return new Color((byte)q, (byte)v, (byte)p);
            else if (hi == 2)
                return new Color((byte)p, (byte)v, (byte)t);
            else if (hi == 3)
                return new Color((byte)p, (byte)q, (byte)v);
            else if (hi == 4)
                return new Color((byte)t, (byte)p, (byte)v);
            else
                return new Color((byte)v, (byte)p, (byte)q);
        }
        public static int Choose(object str)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write(str);
            return Convert.ToInt32(Console.ReadLine());
        }
        public static void Success(object obj) => Log(obj, ConsoleColor.Green);
        public static void Fail(object obj) => Log(obj, ConsoleColor.Red);
        public static void Warn(object obj) => Log(obj, ConsoleColor.Yellow);
        public static void Debug(object obj)
        {
#if DEBUG
            Log(obj, ConsoleColor.Cyan);
#endif
        }
        public static void Log(object obj, ConsoleColor clr = ConsoleColor.White)
        {
            Console.ForegroundColor = clr;
            Console.WriteLine(obj);
        }
        public static string Stringify(byte[] arr)
        {
            string s = "";
            for (int i = 0; i < arr.Length; i++)
                s += arr[i].ToString("x2");
            return s;
        }
        public static string RDecrypt(string text, string key, string iv)
        {
            RijndaelManaged r = new RijndaelManaged()
            {
                Padding = PaddingMode.PKCS7,
                Mode = CipherMode.CBC,
                KeySize = 256,
                BlockSize = 256
            };
            byte[] data = Convert.FromBase64String(text);
            ICryptoTransform d = r.CreateDecryptor(
                Encoding.ASCII.GetBytes(key), Convert.FromBase64String(iv));
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream sm = new CryptoStream(ms, d, CryptoStreamMode.Write))
            {
                sm.Write(data, 0, data.Length);
                sm.FlushFinalBlock();
                return Encoding.ASCII.GetString(ms.ToArray());
            }
        }
        public static string REncrypt(string text, string key, string iv)
        {
            RijndaelManaged r = new RijndaelManaged()
            {
                Padding = PaddingMode.PKCS7,
                Mode = CipherMode.CBC,
                KeySize = 256,
                BlockSize = 256
            };
            byte[] data = Encoding.ASCII.GetBytes(text);
            ICryptoTransform d = r.CreateEncryptor(
                Encoding.ASCII.GetBytes(key), Convert.FromBase64String(iv));
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream sm = new CryptoStream(ms, d, CryptoStreamMode.Write))
            {
                sm.Write(data, 0, data.Length);
                sm.FlushFinalBlock();
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }
    public abstract class HookerBase<T> : IDisposable
        where T : Delegate
    {
        public bool IsHooked = false;
        public virtual void Hook() => IsHooked = true;
        public virtual void Unhook() => IsHooked = false;
        public virtual void Dispose() => Unhook();
    }
    public class Hooker<T> : HookerBase<T>
        where T : Delegate
    {
        public T oFn; T fn;
        IntPtr target, prologAddr;
        int old, size; byte[] hk;
        public Hooker(IntPtr target, T fn, int size = 5)
        {
            this.size = size; this.target = target; this.fn = fn; this.size = size;
            hk = new byte[size];
            byte[] prolog = new byte[5 + size];
            prologAddr = Marshal.AllocHGlobal(5 + size);
            for (int i = 5; i < size; i++) hk[i] = 0;

            hk[0] = prolog[size] = 0xE9;
            IntPtr fnAddr = Marshal.GetFunctionPointerForDelegate(this.fn);
            byte[] fnOff = BitConverter.GetBytes(fnAddr.ToInt32() - target.ToInt32() - 5);
            Array.Copy(fnOff, 0, hk, 1, 4);
            fnOff = BitConverter.GetBytes(target.ToInt32() - prologAddr.ToInt32() - size);
            Array.Copy(fnOff, 0, prolog, size + 1, 4);

            Utility.VirtualProtect(target, (uint)size, 0x40, out old);
            Marshal.Copy(target, prolog, 0, size);
            Utility.VirtualProtect(target, (uint)size, old, out old);

            Marshal.Copy(prolog, 0, prologAddr, 5 + size);
            Utility.VirtualProtect(prologAddr, (uint)(5 + size), 0x40, out old);
            oFn = Marshal.GetDelegateForFunctionPointer<T>(prologAddr);

            if (prologAddr.ToInt32() > target.ToInt32())
                Utility.Fail("lol wut");
        }
        public override void Hook()
        {
            base.Hook();
            Utility.VirtualProtect(target, (uint)size, 0x40, out old);
            Marshal.Copy(hk, 0, target, size);
            Utility.VirtualProtect(target, (uint)size, old, out old);
        }
        public override void Unhook()
        {
            base.Unhook();
            Utility.VirtualProtect(target, (uint)size, 0x40, out old);
            Utility.CopyMemory(target, prologAddr, (uint)size);
            Utility.VirtualProtect(target, (uint)size, old, out old);
        }
        public override void Dispose()
        {
            base.Dispose();
            Marshal.FreeHGlobal(prologAddr);
        }
    }
}
