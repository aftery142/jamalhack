using dnlib.DotNet;
using dnlib.DotNet.Emit;
using EazDecodeLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patcher
{
    class Program
    {
        static ModuleDefMD game = null;
        static Mapping game_mapping = null;
        static int game_version = -1;
        static void Main(string[] args)
        {
            #region Initialize game information
            game = ModuleDefMD.Load(args.Length > 0 ? string.Join(" ", args) : "osu!.exe");
            foreach (TypeDef def in game.Types) {
                MethodDef m = def.FindStaticConstructor();
                int pos = Find(m, SIG_VERSION);
                if (pos != -1) game_version = (int) m.Body.Instructions[pos + SIG_VERSION_OFF].Operand;
            }
            if (game_version == -1) throw new Exception("Game version not found.");
            else Console.WriteLine("[#] Game version: " + game_version);
            game_mapping = new Mapping("3f21fioh321fip231-" + game_version);
            game_mapping.Load(game);
            Console.WriteLine("[+] Generated game mapping.");
            #endregion
            Console.WriteLine("Game information initialized.");
            #region Core initialization
            ModuleDefMD core = ModuleDefMD.Load(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Core.dll"));
            while (core.Types.Count > 0)
            {
                TypeDef def = core.Types[0];
                core.Types.RemoveAt(0);
                if (game.Find(def.FullName, true) == null)
                    game.Types.Add(def);
            }
            Console.WriteLine("[+] Added core types.");
            #endregion
            Console.WriteLine("Core initialized.");
            game_mapping.Remap(game);
            Console.WriteLine("Remapped references.");
            Hook();
            Console.WriteLine("Hooked game functions.");
            #region ????????????
            foreach (TypeDef t in game.GetTypes())
            {
                if (t.FullName.StartsWith("Core")) continue;
                if (t.Visibility == TypeAttributes.NotPublic)
                    t.Visibility = !t.IsNested ? TypeAttributes.Public : TypeAttributes.NestedPublic;
                foreach (MethodDef m in t.Methods)
                {
                    if (m.Access.HasFlag(MethodAttributes.Family))
                    {
                        m.Access &= ~MethodAttributes.Private;
                        m.Access &= ~MethodAttributes.Family;
                        m.Access |= MethodAttributes.Public;
                    }
                    else if (m.Access.HasFlag(MethodAttributes.Private))
                        m.Access |= MethodAttributes.FamANDAssem;
                }
                foreach (FieldDef f in t.Fields)
                {
                    if (f.Access.HasFlag(FieldAttributes.Family))
                    {
                        f.Access &= ~FieldAttributes.Private;
                        f.Access &= ~FieldAttributes.Family;
                        f.Access |= FieldAttributes.Public;
                    }
                    else if (f.Access.HasFlag(FieldAttributes.Private))
                        f.Access |= FieldAttributes.FamANDAssem;
                }
            }
            #endregion
            game.Write("jamal.exe");
            Console.WriteLine("Wrote patched binary to disk.");

            Console.WriteLine("\nExecution complete.");
            Console.ReadLine();
        }
        static void Hook()
        { // Why is this so long?
            game.EntryPoint.Body.Instructions.Insert(0,
                Instruction.Create(OpCodes.Call, game.Find("Core.Main", false).FindMethod("OnInit")));
            Console.WriteLine("[+] Init hooked.");

            game.Find(game_mapping.Encrypt("osu.GameBase"), true).FindMethod(game_mapping.Encrypt("OnExiting"))
                .Body.Instructions.Insert(0,
                    Instruction.Create(OpCodes.Call, game.Find("Core.Main", false).FindMethod("OnExit")));
            Console.WriteLine("[+] Exit hooked.");

            List<Instruction> l; int idx;

            l = ((List<Instruction>)game.Find(game_mapping.Encrypt("osu.OsuMain"), true).FindMethod(game_mapping.Encrypt("get_FullPath"))
                .Body.Instructions);
            if (l[4].OpCode.Code == Code.Call)
            {
                l[4] = Instruction.Create(OpCodes.Call, game.Find("Core.Main", false).FindMethod("GetGamePath"));
                Console.WriteLine("[+] Game path hooked.");
            }
            else throw new Exception("Game path hook failed.");

            l = ((List<Instruction>)game.Find(game_mapping.Encrypt("osu.GameBase"), true)
                .FindMethod(game_mapping.Encrypt("LoadAC")).Body.Instructions);
            l.InsertRange(0, new Instruction[] {
                    Instruction.Create(OpCodes.Call, game.Find("Core.Main", false).FindMethod("OnLoadAC")),
                    Instruction.Create(OpCodes.Brfalse, l[l.Count - 1])
            });
            Console.WriteLine("[+] AC loading hooked.");

            l = ((List<Instruction>)game.Find(game_mapping.Encrypt("osu.GameBase"), true)
                .FindMethod(game_mapping.Encrypt("Draw")).Body.Instructions);
            idx = Find(l, SIG_DRAW);
            if (idx == -1) throw new Exception("Draw hook failed.");
            else l.Insert(idx + SIG_DRAW_OFF,
                Instruction.Create(OpCodes.Call, game.Find("Core.Main", false).FindMethod("OnDraw")));
            Console.WriteLine("[+] Drawing hooked.");

            l = ((List<Instruction>)game.Find(game_mapping.Encrypt("osu.GameplayElements.Scoring.Score"), true)
                .FindMethod(game_mapping.Encrypt("Submit")).Body.Instructions);
            if (l[l.Count - 10].OpCode.Code == Code.Brtrue_S && l[l.Count - 8] == l[l.Count - 10].Operand)
            {
                l.InsertRange(l.Count - 8, new Instruction[] {
                        Instruction.Create(OpCodes.Ldarg_0),
                        Instruction.Create(OpCodes.Call, game.Find("Core.Main", false).FindMethod("OnSubmission")),
                        Instruction.Create(OpCodes.Brfalse, l[l.Count - 1])
                });
                l[l.Count - 13].Operand = l[l.Count - 11];
                Console.WriteLine("[+] Submission hooked.");
            }
            else throw new Exception("Submission hook failed.");

            l = ((List<Instruction>)game.Find(game_mapping.Encrypt("osu.Online.StreamingManager"), true)
                .FindMethod(game_mapping.Encrypt("PushNewFrame")).Body.Instructions);
            if (l[5].OpCode.Code == Code.Ble_S && l[9] == l[5].Operand)
            {
                l.InsertRange(9, new Instruction[] {
                        Instruction.Create(OpCodes.Ldarg_0),
                        Instruction.Create(OpCodes.Call, game.Find("Core.Main", false).FindMethod("OnReplayFramePush")),
                        Instruction.Create(OpCodes.Brfalse, l[l.Count - 1])
                });
                l[5].Operand = l[9];
                Console.WriteLine("[+] Replay frame push hooked.");
            }
            else throw new Exception("Replay frame hook failed.");

            l = ((List<Instruction>)game.Find(game_mapping.Encrypt("osu.Online.BanchoClient"), true)
                .FindMethod(game_mapping.Encrypt("initializePrivate")).Body.Instructions);
            l.Insert(l.Count - 1, Instruction.Create(OpCodes.Call, game.Find("Core.Main", false).FindMethod("OnInitializeHWID")));
            Console.WriteLine("[+] HWID initialization hooked.");

            l = ((List<Instruction>)game.Find(game_mapping.Encrypt("osu_common.Helpers.pWebRequest"), true)
                .FindMethod(game_mapping.Encrypt("perform")).Body.Instructions);
            l.InsertRange(0, new Instruction[]
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Call, game.Find("Core.Main", false).FindMethod("OnWebRequest"))
            });
            Console.WriteLine("[+] Web requests hooked.");

            l = ((List<Instruction>)game.Find(game_mapping.Encrypt("Microsoft.Xna.Framework.Input.Keyboard"), true)
                .FindMethod(game_mapping.Encrypt("GetState")).Body.Instructions);
            if (l[7].OpCode.Code == Code.Pop)
            {
                l.InsertRange(8, new Instruction[]
                {
                    Instruction.Create(OpCodes.Ldloc_1),
                    Instruction.Create(OpCodes.Call, game.Find("Core.Main", false).FindMethod("OnKeyboardInput"))
                });
                Console.WriteLine("[+] Keyboard input hooked.");
            }
            else throw new Exception("Keyboard hook failed.");

            l = ((List<Instruction>)game.Find(game_mapping.Encrypt("osu.GameModes.Play.Player"), true)
                .FindMethod(game_mapping.Encrypt("OnLoadComplete")).Body.Instructions);
            l.InsertRange(0, new Instruction[] {
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Brfalse_S, l[0]),
                Instruction.Create(OpCodes.Call, game.Find("Core.Main", false).FindMethod("OnPlayerLoad"))
            });
            Console.WriteLine("[+] Player load hooked.");

            foreach (TypeDef t in game.GetTypes())
            {
                if (t.FullName.StartsWith("Core")) continue;
                foreach (MethodDef b in t.Methods)
                {
                    if (!b.HasBody) continue;
                    for (int i = 0; i < b.Body.Instructions.Count; i++)
                    {
                        Instruction c = b.Body.Instructions[i];
                        if (c.OpCode.Code == Code.Ldc_R8 && Math.Abs((double)c.Operand - 16.666666666666668) <= 0.0000001)
                        {
                            Console.WriteLine("[+] Found frame time interval: " + c);
                            b.Body.Instructions[i] = Instruction.Create(OpCodes.Call,
                                game.Find("Core.Main", false).FindMethod("GetFrameInterval"));
                        }
                    }
                }
            }
            CilBody bd = game.Find(game_mapping.Encrypt("osu.Graphics.TextureManager"), true)
                .FindMethod(game_mapping.Encrypt("Load")).Body;
            Local tmp = bd.Variables.Add(new Local(bd.Variables[0].Type));
            l = (List<Instruction>)bd.Instructions;
            l.InsertRange(0, new Instruction[] {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Ldarg_2),
                Instruction.Create(OpCodes.Call, game.Find("Core.Main", false).FindMethod("OnTextureLoad")),
                Instruction.Create(OpCodes.Stloc, tmp),
                Instruction.Create(OpCodes.Ldloc, tmp),
                Instruction.Create(OpCodes.Brfalse_S, l[0]),
                Instruction.Create(OpCodes.Ldloc, tmp),
                Instruction.Create(OpCodes.Ret)
            });
            Console.WriteLine("[+] Texture load hooked.");

            l = ((List<Instruction>)game.Find(game_mapping.Encrypt("osu.Online.DiscordStatusManager"), true)
                .FindDefaultConstructor().Body.Instructions);
            idx = Find(l, SIG_DISCORD_ID);
            if (idx == -1) throw new Exception("Discord id hook failed.");
            else
            {
                l[idx + SIG_DISCORD_ID_OFF] = Instruction.Create(OpCodes.Nop);
                l[idx + SIG_DISCORD_ID_OFF + 1] = Instruction.Create(OpCodes.Ldstr, "859355004313010206");
                Console.WriteLine("[+] Discord id hooked.");
            }

            l = ((List<Instruction>)game.Find(game_mapping.Encrypt("osu.Online.DiscordStatusManager"), true)
                .FindMethod(game_mapping.Encrypt("UpdateMatch")).Body.Instructions);
            idx = Find(l, SIG_DISCORD);
            if (idx == -1) throw new Exception("Discord presence hook failed.");
            else
            {
                l.Insert(idx + SIG_DISCORD_OFF,
                    Instruction.Create(OpCodes.Call, game.Find("Core.Main", false).FindMethod("OnDiscordPresenceUpdate")));
                Console.WriteLine("[+] Discord presence update hooked.");
            }
        }
        #region Patterns
        static readonly Code[] SIG_VERSION =
        {
            Code.Ldc_I4, Code.Call, Code.Stsfld,
            Code.Ldc_I4, Code.Call, Code.Stsfld,
            Code.Ldc_I4, Code.Call, Code.Stsfld,
            Code.Ldc_I4, Code.Call, Code.Stsfld,
            Code.Ldc_I4, Code.Call, Code.Stsfld,
            Code.Ldc_I4, Code.Stsfld
        }; static readonly int SIG_VERSION_OFF = 15;
        static readonly Code[] SIG_DRAW =
        {
            Code.Ldsfld, Code.Ldc_I4_2, Code.Bne_Un_S,
            Code.Ldsfld, Code.Ldfld, Code.Ldc_R4,
            Code.Ble_Un_S, Code.Ldsfld, Code.Callvirt,
            Code.Pop, Code.Ldsfld, Code.Callvirt
        }; static readonly int SIG_DRAW_OFF = SIG_DRAW.Length;
        static readonly Code[] SIG_DISCORD_ID =
        {
            Code.Ldarg_0, Code.Ldc_I4, Code.Call,
            Code.Newobj, Code.Stfld
        }; static readonly int SIG_DISCORD_ID_OFF = 1;
        static readonly Code[] SIG_DISCORD =
        {
            Code.Ldfld, Code.Call, Code.Callvirt, Code.Nop,
            Code.Ldarg_0, Code.Ldfld, Code.Ldarg_0, Code.Ldfld, Code.Callvirt
        }; static readonly int SIG_DISCORD_OFF = 8;
        #endregion
        #region Pattern scanning
        static int Find(MethodDef d, Code[] sig)
            => d == null || !d.HasBody ? -1 : Find((List<Instruction>)d.Body.Instructions, sig);
        static int Find(List<Instruction> l, Code[] sig)
        {
            for (int i = 0; i < l.Count - sig.Length; i++)
            {
                bool found = true;
                for (int j = 0; found && j < sig.Length; j++)
                    found = l[i + j].OpCode.Code == sig[j];
                if (found) return i;
            }
            return -1;
        }
        #endregion
    }
}
