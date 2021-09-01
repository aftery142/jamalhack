extern alias sdk;

using Core.Management;
using Core.Feature;
using sdk::osu;
using sdk::osu.GameplayElements.Scoring;
using sdk::osu.Graphics.OpenGl;
using sdk::osu.Graphics.Skinning;
using sdk::osu.Graphics.Sprites;
using sdk::osu_common.Bancho.Objects;
using sdk::osu_common.Helpers;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using sdk::osu.GameModes.Play;
using Core.Type;
using sdk::osu.Audio;
using System.Windows.Forms;
using sdk::osu.Graphics;
using sdk::DiscordRPC;
using sdk::osu.Online.Social;
using Keys = sdk::Microsoft.Xna.Framework.Input.Keys;
using sdk::Newtonsoft.Json;
using System.Diagnostics;
using System.Threading;
using sdk::osu.GameModes.Options;

namespace Core
{ //mega clean code
    public static class Main
    {
        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();
        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();

        private static ClientSettings set = new ClientSettings();
        public static string GetGamePath() => set.GamePath;
        public static bool IsStable() => true;
        public static void OnInit()
        {
            if (!IsStable() && Environment.GetCommandLineArgs().Length < 2)
            {
                Process.Start(Assembly.GetEntryAssembly().Location, "-go");
                Environment.Exit(0);
            }
            AllocConsole();
            Console.Title = "osu!";
            Utility.Success("jamal lol xD");
            Utility.Warn("(i wanna man edition)\n");

            Utility.Log("This is the public & free version of jamalhack.");
            Utility.Log("-> https://github.com/aftery142/jamalhack");
            Utility.Log("If you lost access to the private version,");
            Utility.Log("try contacting me on Telegram/Slack.\n");

            Utility.Debug("Command line args: " + string.Join(" ", Environment.GetCommandLineArgs()));

            Directory.CreateDirectory("jamal");
            Directory.CreateDirectory("jamal/configs");

            #region Load settings
            try
            {
                JsonConvert.PopulateObject
                    (File.ReadAllText("jamal/settings.json"), set);
            }
            catch (FileNotFoundException) {
                if (File.Exists("jamal/path.txt"))
                    set.GamePath = File.ReadAllLines("jamal/path.txt")[0];
                File.WriteAllText("jamal/settings.json",
                    JsonConvert.SerializeObject(set, (Formatting)1));
            }
            catch (Exception e)
            {
                Utility.Fail("Failed to load client settings!");
                Utility.Fail(e);
            }
            #endregion

            #region Validate game path
            if (!File.Exists(set.GamePath))
                set.GamePath = null;
            if (set.GamePath == null && File.Exists("osu!.exe"))
            {
                try
                {
                    set.GamePath = Path.GetFullPath("osu!.exe");
                    Utility.Log("Trying to use osu! executable from current directory.");
                } catch (Exception e)
                {
                    Utility.Warn(e);
                }
            }
            if (set.GamePath != null && !AuthenticodeTools.IsTrusted(set.GamePath))
            {
                set.GamePath = null;
                Utility.Fail("Original osu! executable is not signed!?");
            }
            if (set.GamePath == null) {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "osu! executable|osu!.exe";
                ofd.CheckPathExists = true;
                ofd.InitialDirectory = Assembly.GetEntryAssembly().Location;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    set.GamePath = ofd.FileName;
                    File.WriteAllText("jamal/settings.json",
                        JsonConvert.SerializeObject(set, (Formatting)1));
                    Utility.Success("Please reopen game!");
                    Console.ReadLine();
                }
            }
            if (set.GamePath == null) Environment.Exit(0);
            #endregion
            Utility.Debug("Game path: " + set.GamePath);

            ConfigSystem.Save("default");
            ConfigSystem.Load("current");

            new Keybind((x, y) =>
            {
                if (!y) return;
                ChatEngine.HandleMessage(new bMessage("BanchoBot", "BanchoBot",
                    "Your account is currently in restricted mode. Please visit the osu! website for more information."), false, true);
            }, Keys.End);

            HWIDSpoof.Load();
            Modifiers.Init();
            Submissions.Init();
            ConfigSystem.Init();

            if (!set.SafeMode)
            {
                #region Hooking
                IntPtr gl = Utility.LoadLibrary("opengl32.dll");
                IntPtr swap = Utility.GetProcAddress(gl, "wglSwapBuffers");
                IntPtr pxl = Utility.GetProcAddress(gl, "glReadPixels");
                if (swap == IntPtr.Zero) Utility.Fail("wglSwapBuffers was not found!");
                else
                {
                    hkmwglSwapBuffers = new Hooker<fnwglSwapBuffers>(swap, hkwglSwapBuffers);
                    hkmwglSwapBuffers.Hook();
                    Utility.Debug("wglSwapBuffers hooked.");
                }
                if (pxl == IntPtr.Zero) Utility.Fail("glReadPixels was not found!");
                else
                {
                    hkmglReadPixels = new Hooker<fnglReadPixels>(pxl, hkglReadPixels);
                    hkmglReadPixels.Hook();
                    Utility.Debug("glReadPixels hooked.");
                }
                #endregion
            }
            else Utility.Warn("Safe mode is on. Streamproof rendering will be unavailable.");

            SkinManager.add_OnSkinChanged(OnSkinChanged);

            if (set.DisableConsole)
            {
                FreeConsole();
                //Console.SetIn(new StreamReader(Console.OpenStandardInput()));
                //Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
                //Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
            }
        }
        public static void OnExit()
        {
            ConfigSystem.Save("current");

            if (hkmwglSwapBuffers != null && hkmwglSwapBuffers.IsHooked) hkmwglSwapBuffers.Unhook();
            if (hkmglReadPixels != null && hkmglReadPixels.IsHooked) hkmglReadPixels.Unhook();
            if (hkmCreateFileW != null && hkmCreateFileW.IsHooked) hkmCreateFileW.Unhook();

            FreeConsole();
        }
        public static bool OnLoadAC()
        {
            string md5 = CryptoHelper.GetMd5("osu!auth.dll");
            if (!string.IsNullOrEmpty(md5))
            {
                #region CreateFile hook
                string asmb = Assembly.GetEntryAssembly().Location;
                File.Copy(GetGamePath(), Path.Combine(
                    Path.GetDirectoryName(asmb), Path.GetFileName(asmb) + ".oo"), true);
                IntPtr k32 = Utility.LoadLibrary("kernelbase.dll");
                IntPtr cfw = Utility.GetProcAddress(k32, "CreateFileW");
                if (cfw == IntPtr.Zero) Utility.Fail("CreateFileW was not found! osu!auth disabler won't work.");
                else
                {
                    hkmCreateFileW = new Hooker<fnCreateFileW>(cfw, hkCreateFileW);
                    hkmCreateFileW.Hook();
                    Utility.Debug("CreateFileW hooked.");
                }
                #endregion
                if (md5 != "543daf5f2b96662c138c0b25663a66e5"
                    && md5 != "e2109a27efb428af69318c41fc4d96c8")
                    Utility.Warn("osu!auth.dll version is unknown. It might be unsafe to submit scores.");
                if (Utility.LoadLibrary("auth_emu.dll") != IntPtr.Zero)
                { // fixed! now works on the latest version (hopefully).
                    Utility.Success("hello :)");
                    return false;
                }
                return true;
            }
            else return false;
        }
        private static bool CanDraw = true;
        public static void ActualOnDraw()
        {
            Modifiers.Draw();
            Submissions.Draw();
        }
        public static void OnDraw()
        {
            OsuMain.startupValue = 0;
            if (Player.Instance != null) {
                Player.Instance.audioCheckTime = AudioEngine.Time;
                Player.Instance.dateTimeCheckTimeInitial = DateTime.Now.Ticks / 10000L;
                Player.Instance.dateTimeCheckTimeComp 
                    = Player.Instance.audioCheckTimeComp = GameBase.Time;
                Player.Instance.audioCheckCount = 0;
                if (!Player.Instance.flCheckedThisPlay && Player.Instance.flSkippedThisNote < 9)
                {
                    Player.Instance.flCheckedThisPlay = true;
                    Player.Instance.flSkippedThisNote = 0;
                }
                if (Miscellaneous.PauseDelay && AudioEngine.Time - Player.Instance.LastPause < 1000)
                    Player.Instance.LastPause = AudioEngine.Time - 1000;
                if (Modifiers.TD == Modifiers.YesNoMode.Yes)
                {
                    if (Player.Instance.jumpCount < 10) Player.Instance.jumpCount = 11;
                }
                else if (Modifiers.TD == Modifiers.YesNoMode.No)
                    Player.Instance.jumpCount = 0;
            }
            Player.mouseMovementDiscrepancyInMenu = true;
            if (Player.flag != 0)
            {
                Utility.Debug("Reset flag from " + Player.flag);
                Player.flag = 0;
            }
            Modifiers.UpdateRate();
            if (CanDraw && (!Miscellaneous.StreamProof 
                    || hkmwglSwapBuffers == null || !hkmwglSwapBuffers.IsHooked)) ActualOnDraw();
        }
        public static bool OnSubmission(Score s)
        {
            Utility.Debug("Score submission: " + s.Beatmap.DisplayTitle + ", flag: " + Player.flag);
            Player.flag = 0;
            return Submissions.OnSubmit(s);
        }
        public static bool OnReplayFramePush(bReplayFrame f)
            => Miscellaneous.Apply(f);
        public static void OnInitializeHWID()
            => HWIDSpoof.UpdateHWID();
        public static void OnWebRequest(pWebRequest req)
		{
            if (req.Parameters.Count > 0)
            {
                Utility.Debug("Web request: " + req.get_Url() + ", parameter count: " + req.Parameters.Count);
                foreach (var x in req.Parameters)
                    Utility.Debug(x.Key + ": " + x.Value);
            }
            HWIDSpoof.FixWebRequest(req);
            Submissions.Update(req);
		}
        public static void OnKeyboardInput(byte[] keys)
        {
            InputSystem.Update(keys);
        }
        public static void OnPlayerLoad()
        {
            Utility.Debug("Player loaded.");
        }
        public static double GetFrameInterval()
            => Modifiers.AdjustFrameInterval(16.666666666666668);
        public static pTexture OnTextureLoad(string name, SkinSource source, TextureAtlas atlas)
        {
            if (source != (SkinSource)1 || !Miscellaneous.Skinny
                || name == null || name.StartsWith("button")) return null;
            string x = name.StartsWith("menu-background") ? "menu-background" : name;
            pTexture tex = null;
            try
            {
                tex = TextureManager.Load(x, (SkinSource)2, atlas);
            } catch (Exception e)
            {
                return null;
            }
            if (tex != null)
                Utility.Debug("Loaded texture: " + x);
            return tex;
        }
        public static string GetDiscordID(string s)
            => DiscordPresence.GetID(s);
        public static RichPresence OnDiscordPresenceUpdate(RichPresence p)
            => DiscordPresence.Update(p);
        public static bool AllowGameUpdates()
            => set.GameUpdates;
        public static void OnSkinChanged()
        {
            Utility.Debug("Skin changed.");
        }
        #region wglSwapBuffers hooking
        [DllImport("opengl32.dll", SetLastError = true)]
        static extern bool wglSwapBuffers(IntPtr hdc);
        delegate bool fnwglSwapBuffers(IntPtr hdc);
        static Hooker<fnwglSwapBuffers> hkmwglSwapBuffers;
        static bool hkwglSwapBuffers(IntPtr hdc)
        {
            if (CanDraw && Miscellaneous.StreamProof) {
                SpriteManager.NewFrame();
                OsuGlControl.TextureShader2D.Begin();
                ActualOnDraw();
                OsuGlControl.TextureShader2D.End();
            }
            bool ret = hkmwglSwapBuffers.oFn(hdc);
            return ret;
        }
        #endregion
        #region glReadPixels hooking
        [DllImport("opengl32.dll")]
        static extern bool glReadPixels(int x, int y, int width, int height, int format, int type, IntPtr pixels);
        delegate void fnglReadPixels(int x, int y, int width, int height, int format, int type, IntPtr pixels);
        static Hooker<fnglReadPixels> hkmglReadPixels;
        static void hkglReadPixels(int x, int y, int width, int height, int format, int type, IntPtr pixels)
        {
            Utility.Debug("ReadPixels intercepted.");

            CanDraw = false;
            try
            {
                GameBase.Instance.Draw();
                GameBase.Instance.Draw();
                hkmglReadPixels.oFn(x, y, width, height, format, type, pixels);
            } catch (Exception e)
            {
                Utility.Fail(e);
            } finally
            {
                CanDraw = true;
            }
        }
        #endregion
        #region CreateFileW hooking
        [DllImport("kernel32.dll")]
        static extern IntPtr CreateFileW(
             string filename,
             int access,
             int share,
             IntPtr securityAttributes,
             int creationDisposition,
             int flagsAndAttributes,
             IntPtr templateFile);
        delegate IntPtr fnCreateFileW(
             [MarshalAs(UnmanagedType.LPWStr)] string filename,
             int access,
             int share,
             IntPtr securityAttributes,
             int creationDisposition,
             int flagsAndAttributes,
             IntPtr templateFile);
        static Hooker<fnCreateFileW> hkmCreateFileW;
        static IntPtr hkCreateFileW(
             string filename,
             int access,
             int share,
             IntPtr securityAttributes,
             int creationDisposition,
             int flagsAndAttributes,
             IntPtr templateFile)
        {
            if (filename.Contains(".exe") || filename.Contains(".dll"))
            {
                IntPtr tmp = hkmCreateFileW.oFn(filename + ".oo", access, share, securityAttributes,
                    creationDisposition, flagsAndAttributes, templateFile);
                if (tmp.ToInt32() != -1)
                {
                    Utility.Debug("File swapped: " + filename);
                    return tmp;
                }
                filename = filename.ToLower();
                if (filename.Contains("dbg") || filename.Contains("cheat") || filename.Contains("hack"))
                {
                    Utility.Debug("File hidden: " + filename);
                    return new IntPtr(-1);
                }
            }
            return hkmCreateFileW.oFn(filename, access, share, securityAttributes,
                creationDisposition, flagsAndAttributes, templateFile);
        }
        #endregion
    }
}
